using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Hosting.Abstractions.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceDataSets.Endpoints.Logging;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Generic query endpoint for any registered DataSet.
/// GET /datasets/{DataSetName}/query?field1=value1&amp;field2=value2&amp;skip=0&amp;take=50
/// </summary>
public class QueryDataSetEndpoint : Endpoint<QueryDataSetRequest, QueryDataSetResponse>
{
    private static readonly HashSet<string> ReservedParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "DataSetName", "skip", "take"
    };

    // Why: IDataSetConfigurationProvider (FDW-439) returns DataSetConfiguration directly —
    // replaces old IDataSetProvider which returned IGenericConfiguration requiring a cast.
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly IDataGateway _dataGateway;
    private readonly IConfigurationConnectionNameProvider _connectionNameProvider;
    private readonly ILogger<QueryDataSetEndpoint> _logger;

    public QueryDataSetEndpoint(
        IDataSetConfigurationProvider dataSetProvider,
        IDataGateway dataGateway,
        IConfigurationConnectionNameProvider connectionNameProvider,
        ILogger<QueryDataSetEndpoint> logger)
    {
        _dataSetProvider = dataSetProvider;
        _dataGateway = dataGateway;
        _connectionNameProvider = connectionNameProvider;
        _logger = logger ?? NullLogger<QueryDataSetEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("/datasets/{DataSetName}/query");
        Tags("DataSets");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Query a DataSet";
            s.Description = "Queries a registered DataSet by name with optional field filters as query parameters. " +
                            "Pass any DataSet field name as a query parameter to filter results (e.g., ?TeamName=Eagles). " +
                            "See the DataSetQueryDocumentation tag for per-dataset field details.";
        });
    }

    public override async Task HandleAsync(QueryDataSetRequest req, CancellationToken ct)
    {
        var take = Math.Clamp(req.Take, 1, 1000);
        var skip = Math.Max(req.Skip, 0);

        DataSetQueryLog.QueryingDataSet(_logger, req.DataSetName, skip, take);

        // Resolve DataSet configuration by name
        DataSetQueryLog.ResolvingDataSet(_logger, req.DataSetName);
        var dataSetResult = await _dataSetProvider.Get(req.DataSetName, ct).ConfigureAwait(false);
        if (!dataSetResult.IsSuccess || dataSetResult.Value is null)
        {
            DataSetQueryLog.DataSetNotFound(_logger, req.DataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var dataSet = dataSetResult.Value;
        var fields = dataSet.Fields;
        DataSetQueryLog.FieldsResolved(_logger, req.DataSetName, fields.Count);

        // Extract field filters from query string
        var fieldNames = new HashSet<string>(
            fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        var appliedFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var queryParam in HttpContext.Request.Query)
        {
            if (ReservedParams.Contains(queryParam.Key))
                continue;

            if (fieldNames.Contains(queryParam.Key))
            {
                var value = queryParam.Value.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    appliedFilters[queryParam.Key] = value;
                }
            }
            else
            {
                DataSetQueryLog.UnknownFilterField(_logger, req.DataSetName, queryParam.Key);
            }
        }

        DataSetQueryLog.ApplyingFilters(_logger, req.DataSetName, appliedFilters.Count);

        // Resolve primary source (highest-priority).
        // Why: Sources are part of the composed aggregate returned by DataSetConfigurationProvider.Get —
        // no separate resolver call needed in 1.6.0+.
        DataSetQueryLog.ResolvingSource(_logger, req.DataSetName);
        var primarySource = dataSet.Sources
            .Where(s => s.IsCurrent && !s.IsDeleted)
            .OrderBy(s => s.Priority)
            .FirstOrDefault();

        if (primarySource is null)
        {
            DataSetQueryLog.NoSourceFound(_logger, req.DataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(primarySource.ContainerName))
        {
            DataSetQueryLog.NoSourceFound(_logger, req.DataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        DataSetQueryLog.ExecutingQuery(_logger, req.DataSetName, primarySource.SourceName);

        // Build filter expression
        var filter = BuildFilter(appliedFilters);

        // Build query command. Take+1 rows so an unconsumed extra row signals "more pages exist"
        // without a second COUNT round-trip; Skip/Take push down to SQL OFFSET/FETCH via the translator.
        var dataStoreName = string.IsNullOrEmpty(primarySource.DataStoreName)
            ? _connectionNameProvider.ConnectionName
            : primarySource.DataStoreName;
        var command = new QueryCommand<object>()
        {
            Paging = new PagingExpression { Skip = skip, Take = take + 1 },
            Filter = filter
        };

        // Why: stream the result through the record-source cursor instead of materializing a list of
        // ExpandoObject/Dictionary rows. The cursor describes its columns ONCE (a shared schema flyweight)
        // and yields each row as a DataRecord over a single object?[] — no per-row name→value dictionary.
        var cursorResult = await _dataGateway
            .OpenRecordSource(command, new DataStoreTarget(dataStoreName, primarySource.PathValue, primarySource.ContainerName), ct)
            .ConfigureAwait(false);

        if (cursorResult.IsFailure || cursorResult.Value is null)
        {
            DataSetQueryLog.SourceQueryFailed(_logger, req.DataSetName,
                cursorResult.CurrentMessage ?? "Unknown error");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Why: the cursor OWNS the open connection/reader — await using guarantees the connection is
        // released even if streaming throws or the client disconnects mid-iteration.
        await using var cursor = cursorResult.Value;

        // Build column metadata from the cursor's physical schema (the exact SELECT column order, so row
        // cell i == Columns[i]), enriched once with the dataset field metadata matched by name.
        var columns = new List<QueryDataSetColumnDto>(cursor.Schema.Fields.Count);
        foreach (var cursorField in cursor.Schema.Fields)
        {
            var column = new QueryDataSetColumnDto { Name = cursorField.Name };
            var datasetField = fields.FirstOrDefault(f => string.Equals(f.Name, cursorField.Name, StringComparison.OrdinalIgnoreCase));
            if (datasetField is not null)
            {
                column.DataType = datasetField.TypeName;
                column.IsKey = datasetField.IsKey;
                column.IsIndexed = datasetField.IsIndexed;
                column.Role = datasetField.Role;
            }

            columns.Add(column);
        }

        var rows = new List<object?[]>();
        var hasMore = false;
        await foreach (var recordResult in cursor.Read(ct).ConfigureAwait(false))
        {
            if (recordResult.IsFailure)
            {
                DataSetQueryLog.SourceQueryFailed(_logger, req.DataSetName,
                    recordResult.CurrentMessage ?? "Unknown error");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            // Why: the take+1'th row only proves more pages exist — don't emit it.
            if (rows.Count >= take)
            {
                hasMore = true;
                break;
            }

            // Why: copy the cursor's shared value buffer into a row-owned array — the cursor reuses/advances
            // its buffer per record, so the response must own its own snapshot. One object?[] per row, no keys.
            rows.Add(recordResult.Value.Values.ToArray());
        }

        DataSetQueryLog.QueryCompleted(_logger, req.DataSetName, rows.Count, hasMore);

        await Send.OkAsync(new QueryDataSetResponse
        {
            DataSetName = req.DataSetName,
            Columns = columns,
            Rows = rows,
            Skip = skip,
            Take = take,
            HasMoreRows = hasMore,
            AppliedFilters = appliedFilters
        }, ct).ConfigureAwait(false);
    }

    private static FilterExpression? BuildFilter(Dictionary<string, string> filters)
    {
        if (filters.Count == 0)
            return null;

        var conditions = new List<IFilterNode>();
        foreach (var (fieldName, value) in filters)
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = fieldName,
                Operator = FilterOperators.ByName("Equal"),
                Value = value
            });
        }

        if (conditions.Count == 1)
        {
            return new FilterExpression { Root = conditions[0] };
        }

        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = conditions
            }
        };
    }
}
