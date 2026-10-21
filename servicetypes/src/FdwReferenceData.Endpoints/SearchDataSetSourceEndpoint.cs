using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to search for DataSets available to bind as a source for compound/federated composition.
/// Excludes the parent DataSet itself from results.
/// </summary>
[ExcludeFromCodeCoverage]
public class SearchDataSetSourceEndpoint : Endpoint<SearchDataSetSourceRequest, List<DataSetSourceSearchResultDto>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly CategoryConfigurationProvider _categories;
    private readonly ILogger<SearchDataSetSourceEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchDataSetSourceEndpoint"/> class.
    /// </summary>
    public SearchDataSetSourceEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        CategoryConfigurationProvider categories,
        ILogger<SearchDataSetSourceEndpoint> logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories;
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

        // Records only: a candidate is matched and described by its own row (FDW-951).
        var allResult = await _dataSetProvider.Get(ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
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

        var tree = await DataSetCategoryLinks.Tree(_categories, _logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var results = new List<DataSetSourceSearchResultDto>();
        foreach (var ds in candidates.OrderBy(ds => ds.Name, System.StringComparer.OrdinalIgnoreCase))
        {
            var label = DataSetCategoryLinks.Label(tree.Value, ds.CategoryId, _logger);
            if (label.IsFailure || label.Value is null)
            {
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            results.Add(new DataSetSourceSearchResultDto
            {
                Id = ds.Id,
                Name = ds.Name,
                DisplayName = ds.DisplayName ?? ds.Name,
                Category = label.Value.Name,
                CategoryPath = label.Value.Path,
            });
        }

        DataSetEndpointLog.DataSetsLoaded(_logger, results.Count);

        await Send.OkAsync(results, ct).ConfigureAwait(false);
    }
}
