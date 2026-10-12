using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Pagination;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceDataSets.Endpoints.Logging;

namespace FdwReferenceDataSets.Endpoints;

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

    // Why: IDataSetConfigurationProvider (FDW-439) returns DataSetImplementationConfiguration directly —
    // replaces old IDataSetProvider which returned IGenericConfiguration requiring a cast.
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<QueryDataSetEndpoint> _logger;

    public QueryDataSetEndpoint(
        IDataSetConfigurationProvider dataSetProvider,
        IDataGatewayProvider dataGateways,
        ILogger<QueryDataSetEndpoint> logger)
    {
        _dataSetProvider = dataSetProvider;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<QueryDataSetEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("/datasets/{DataSetName}/query");
        Tags("DataSets");
        // Why gated: this GET twin of the former POST /datasets/{DataSetName}/query (now POST
        // /data/{DataSetName}, FDW-975) still declared AllowAnonymous() unconditionally after FDW-876 closed the POST one, so an unauthenticated caller could read any
        // shared data set's rows through the query string (FDW-910). Same DEVELOP-only shape as every sibling.
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        // Why the metadata: a caller no page limit applies to is answered by streaming every row, and the buffering
        // middleware would otherwise hold that response to set a Content-Length (FDW-1209).
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));
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
        DataSetQueryLog.ResolvingDataSet(_logger, req.DataSetName);
        var dataSetResult = await _dataSetProvider.Get(req.DataSetName, ct).ConfigureAwait(false);
        if (!dataSetResult.IsSuccess || dataSetResult.Value is null)
        {
            DataSetQueryLog.DataSetNotFound(_logger, req.DataSetName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var dataSet = dataSetResult.Value;
        var declared = dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted).ToList();
        DataSetQueryLog.FieldsResolved(_logger, req.DataSetName, declared.Count);

        var appliedFilters = AppliedFilters(req.DataSetName, declared);
        DataSetQueryLog.ApplyingFilters(_logger, req.DataSetName, appliedFilters.Count);

        // Why the paging settings and not a clamp of 1..1000 (FDW-1209): this route read its own limits, so it cut every
        // caller at 1000, an API key included, and no setting could change it. After the data set is resolved, as POST
        // /data/{name} does, so a data set that does not exist still answers 404.
        var paging = PaginationQuery.Parse(HttpContext, _logger);
        if (paging.IsFailure)
        {
            await SendFailure(paging).ConfigureAwait(false);
            return;
        }

        var rowCap = PaginationQuery.ResolveCalculatedRowCap(HttpContext, _logger);
        if (rowCap.IsFailure)
        {
            await SendFailure(rowCap).ConfigureAwait(false);
            return;
        }

        var (skip, take) = (paging.Value!.Skip, paging.Value.Take);
        DataSetQueryLog.QueryingDataSet(_logger, req.DataSetName, skip, take);

        // Why through DataSetQuery (FDW-1075): the same read POST /data/{name} makes -- the data set's read gate, its
        // renames, transformations and calculated fields, the filterable check, and only its declared fields. Reading the
        // first source's container directly answered with every column of the table under the data set, raw.
        var request = new DataQueryRequest { DataSetName = req.DataSetName, Skip = skip, Take = take, Filter = Filter(appliedFilters) };
        if (paging.Value.IsUnlimited)
        {
            await Stream(request, dataSet, declared, appliedFilters, rowCap.Value, ct).ConfigureAwait(false);
            return;
        }

        var page = await new DataSetQuery(_dataGateways, _logger)
            .Run(dataSet, request, take, rowCap.Value, DataSetQuery.Unscoped, ct)
            .ConfigureAwait(false);
        if (page.IsFailure)
        {
            await SendFailure(page).ConfigureAwait(false);
            return;
        }

        var read = page.Value!;
        DataSetQueryLog.QueryCompleted(_logger, req.DataSetName, read.Rows.Count, read.HasMoreRows);
        await Send.OkAsync(PageResponse(read, declared, skip, appliedFilters), ct).ConfigureAwait(false);
    }

    // The page in this route's shape: each row as its values in column order, the columns as the data set declares them.
    private static QueryDataSetResponse PageResponse(
        DataQueryPage read, List<DataSetFieldConfiguration> declared, int skip, Dictionary<string, string> appliedFilters) => new()
    {
        DataSetName = read.DataSetName,
        Columns = [.. read.Columns.Select(c => Column(c, declared))],
        Rows = [.. read.Rows.Select(row => read.Columns.Select(c => row.TryGetValue(c.Name, out var value) ? value : null).ToArray())],
        Skip = skip,
        Take = read.Take,
        HasMoreRows = read.HasMoreRows,
        AppliedFilters = appliedFilters
    };

    // Every row, written as it is read, for a caller no page limit applies to (FDW-1209): the same batches POST /data/{name}
    // reads, in this route's shape. The first batch is read before the response starts, so a refusal still has a status.
    private async Task Stream(
        DataQueryRequest request, IDataSetImplementationConfiguration dataSet, List<DataSetFieldConfiguration> declared,
        Dictionary<string, string> appliedFilters, int rowCap, CancellationToken ct)
    {
        var batchSize = PaginationQuery.ResolveBatchSize(HttpContext, _logger);
        if (batchSize.IsFailure)
        {
            await SendFailure(batchSize).ConfigureAwait(false);
            return;
        }

        var opened = await DataQueryStream.Open(
            new DataSetQuery(_dataGateways, _logger), dataSet, request, batchSize.Value, rowCap, DataSetQuery.Unscoped, _logger, ct)
            .ConfigureAwait(false);
        if (opened.IsFailure)
        {
            await SendFailure(opened).ConfigureAwait(false);
            return;
        }

        var stream = opened.Value!;
        var response = new QueryDataSetStreamResponse(
            stream.DataSetName,
            [.. stream.Columns.Select(c => Column(c, declared))],
            request.Skip,
            appliedFilters,
            Positional(stream),
            () => stream.RowCount,
            () => stream.Complete);

        try
        {
            await Send.ResultAsync(TypedResults.Json(response)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The client going away is the client's decision, and there is nobody left to tell.
            HttpContext.Abort();
        }
        catch (Exception ex)
        {
            // Why aborted, and never answered with a failure status (FDW-1209): the serializer has already written some of the
            // body, flushed or not, and a failure written after it would be a second document in the same body. A cut
            // connection is what tells the client the rows are incomplete, where a closed body would read as a whole one with
            // a 200 on it. The stream logged its own failure; anything else is logged here.
            if (DataQueryStream.CausedBy(ex) is null)
                DataSetQueryLog.StreamAborted(_logger, ex, request.DataSetName);

            HttpContext.Abort();
        }
    }

    // Each row as its values in column order, as the paged response gives them.
    private static async IAsyncEnumerable<object?[]> Positional(DataQueryStream stream)
    {
        await foreach (var row in stream.Rows.ConfigureAwait(false))
            yield return [.. stream.Columns.Select(c => row.TryGetValue(c.Name, out var value) ? value : null)];
    }

    // The query string's field filters: a parameter naming a declared field filters on it; any other is not one and is said so.
    private Dictionary<string, string> AppliedFilters(string dataSetName, List<DataSetFieldConfiguration> declared)
    {
        var fieldNames = new HashSet<string>(declared.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        var appliedFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var queryParam in HttpContext.Request.Query)
        {
            if (ReservedParams.Contains(queryParam.Key))
                continue;

            if (!fieldNames.Contains(queryParam.Key))
            {
                DataSetQueryLog.UnknownFilterField(_logger, dataSetName, queryParam.Key);
                continue;
            }

            var value = queryParam.Value.ToString();
            if (!string.IsNullOrEmpty(value))
                appliedFilters[queryParam.Key] = value;
        }

        return appliedFilters;
    }

    private Task<FastEndpoints.Void> SendFailure(IGenericResult failure)
    {
        var (status, problem) = ResultHttpStatusMapper.Map(failure, HttpContext);
        return Send.ResultAsync(TypedResults.Json(problem, statusCode: status));
    }

    private static QueryDataSetColumnDto Column(DataQueryColumnDto column, List<DataSetFieldConfiguration> declared) => new()
    {
        Name = column.Name,
        DataType = column.DataType,
        IsKey = column.IsKey,
        IsIndexed = declared.Any(f => f.IsIndexed && string.Equals(f.Name, column.Name, StringComparison.OrdinalIgnoreCase)),
        IsFilterable = column.IsFilterable,
        FilterOperators = column.FilterOperators,
        Role = column.Role
    };

    // Each query-string filter is an Equal condition on its field, all of them ANDed: the same filter tree POST /data reads,
    // so its value is converted to the field's type and a field that is not filterable is refused by name.
    private static QueryFilterNode? Filter(Dictionary<string, string> filters)
    {
        if (filters.Count == 0)
            return null;

        return new QueryFilterNode
        {
            Logic = "And",
            Nodes = [.. filters.Select(f => new QueryFilterNode { Field = f.Key, Operator = "Equal", Value = JsonSerializer.SerializeToElement(f.Value) })]
        };
    }
}
