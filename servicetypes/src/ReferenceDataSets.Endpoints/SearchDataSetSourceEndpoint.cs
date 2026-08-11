using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to search for DataSets available to bind as a source for compound/federated composition.
/// Excludes the parent DataSet itself from results.
/// </summary>
[ExcludeFromCodeCoverage]
public class SearchDataSetSourceEndpoint : Endpoint<SearchDataSetSourceRequest, List<DataSetSourceSearchResultDto>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<SearchDataSetSourceEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchDataSetSourceEndpoint"/> class.
    /// </summary>
    public SearchDataSetSourceEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<SearchDataSetSourceEndpoint> logger)
    {
        _dataSetProvider = dataSetProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/datasets/{ParentDataSetName}/sources/search");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Search for DataSets to bind as a source";
            s.Description = "Returns DataSets available to bind as a source in compound/federated composition. Excludes the parent DataSet.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(SearchDataSetSourceRequest req, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingSources(_logger, req.ParentDataSetName);

        var allResult = await _dataSetProvider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Why: IsSuccess was verified above; Value is non-null on success.
        var allDataSets = allResult.Value;
        if (allDataSets is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var candidates = allDataSets
            .Where(ds => !string.Equals(ds.Name, req.ParentDataSetName, System.StringComparison.OrdinalIgnoreCase))
            .AsEnumerable();

        if (!string.IsNullOrEmpty(req.SearchTerm))
        {
            candidates = candidates.Where(ds =>
                ds.Name.Contains(req.SearchTerm, System.StringComparison.OrdinalIgnoreCase) ||
                (ds.DisplayName != null && ds.DisplayName.Contains(req.SearchTerm, System.StringComparison.OrdinalIgnoreCase)));
        }

        var results = candidates
            .OrderBy(ds => ds.Name, System.StringComparer.OrdinalIgnoreCase)
            .Select(ds => new DataSetSourceSearchResultDto
            {
                Id = ds.Id,
                Name = ds.Name,
                DisplayName = ds.DisplayName ?? ds.Name,
                Category = ds.Category
            })
            .ToList();

        DataSetEndpointLog.DataSetsLoaded(_logger, results.Count);

        await Send.OkAsync(results, ct).ConfigureAwait(false);
    }
}
