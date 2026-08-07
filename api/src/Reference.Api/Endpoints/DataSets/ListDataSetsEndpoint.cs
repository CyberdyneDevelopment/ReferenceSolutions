using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to list all DataSets.
/// Uses DataSetConfigurationProvider for base data, then supplements with field/source counts from the database.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetsEndpoint : ListDataSetsEndpointBase
{
    // Why: DataSetConfigurationProvider (FDW-439) returns IReadOnlyList<DataSetConfiguration> directly —
    // replaces old IDataSetProvider which returned IReadOnlyList<IGenericConfiguration> requiring OfType cast.
    private readonly DataSetConfigurationProvider _dataSetProvider;
    // Why: IDataGateway is injected directly here (not via base) because the base no longer holds a
    // gateway reference — config reads go through the provider. The gateway here is only for the
    // supplemental DB queries (field counts, source counts, real IDs) that are app-specific behaviour.
    private readonly IDataGateway _dataGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataSetsEndpoint"/> class.
    /// </summary>
    public ListDataSetsEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        IDataGateway dataGateway)
        : base(dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
        _dataGateway = dataGateway;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataSetSummaryResponse>>> LoadDataSetSummaries(CancellationToken ct)
    {
        var allDataSetsResult = await _dataSetProvider.Get(ct).ConfigureAwait(false);
        if (!allDataSetsResult.IsSuccess || allDataSetsResult.Value is null)
            return allDataSetsResult.ToNewResult<List<DataSetSummaryResponse>>();

        var summaries = allDataSetsResult.Value.Select(ds => new DataSetSummaryResponse
        {
            Name = ds.Name,
            Description = ds.Description,
            Category = ds.Category,
            Version = ds.Version,
            FieldCount = ds.Fields.Count,
            SourceCount = ds.SourceIds.Count,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = string.Empty,
            ModifiedBy = string.Empty,
            CreatedOnBehalfOf = string.Empty,
            ModifiedOnBehalfOf = string.Empty
        }).ToList();

        var activeFilter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                    new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false }
                ]
            }
        };

        // Query DataSet table to get real IDs and map name→id
        var dsCommand = new QueryCommand<Dictionary<string, object?>>()
        {
            Filter = activeFilter
        };
        var dsResult = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(dsCommand, new DataStoreTarget(_dataSetProvider.DataStoreName, _dataSetProvider.PathName, "DataSet"), ct).ConfigureAwait(false);

        var nameToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (dsResult.IsSuccess && dsResult.Value != null)
        {
            foreach (var row in dsResult.Value)
            {
                var name = row.GetValueOrDefault("Name")?.ToString() ?? string.Empty;
                var id = row.GetValueOrDefault("Id")?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
                {
                    nameToId[name] = id;
                }
            }

            // Populate real IDs on summaries
            foreach (var summary in summaries)
            {
                if (nameToId.TryGetValue(summary.Name, out var realId) && Guid.TryParse(realId, out var guid))
                {
                    summary.Id = guid;
                }
            }
        }

        // Query field counts
        var fieldCommand = new QueryCommand<Dictionary<string, object?>>()
        {
            Filter = activeFilter
        };
        var fieldResult = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(fieldCommand, new DataStoreTarget(_dataSetProvider.DataStoreName, _dataSetProvider.PathName, "DataSetField"), ct).ConfigureAwait(false);

        if (fieldResult.IsSuccess && fieldResult.Value != null)
        {
            var fieldCounts = fieldResult.Value
                .GroupBy(f => f.GetValueOrDefault("DataSetId")?.ToString() ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var summary in summaries)
            {
                if (fieldCounts.TryGetValue(summary.Id.ToString(), out var count))
                {
                    summary.FieldCount = count;
                }
            }
        }

        // Query source counts
        var sourceCommand = new QueryCommand<Dictionary<string, object?>>()
        {
            Filter = activeFilter
        };
        var sourceResult = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(sourceCommand, new DataStoreTarget(_dataSetProvider.DataStoreName, _dataSetProvider.PathName, "DataSetSource"), ct).ConfigureAwait(false);

        if (sourceResult.IsSuccess && sourceResult.Value != null)
        {
            var sourceCounts = sourceResult.Value
                .GroupBy(s => s.GetValueOrDefault("DataSetId")?.ToString() ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var summary in summaries)
            {
                if (sourceCounts.TryGetValue(summary.Id.ToString(), out var count))
                {
                    summary.SourceCount = count;
                }
            }
        }

        return GenericResult<List<DataSetSummaryResponse>>.Success(summaries);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
