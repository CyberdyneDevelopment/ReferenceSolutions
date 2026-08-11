using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Concrete endpoint to search the data catalog.
/// </summary>
/// <remarks>
/// The catalogue IS the datasets + datastores the operator can build from (the workbench entry point):
/// this override enumerates both straight from their providers (<see cref="DataSetConfigurationProvider"/>
/// and <see cref="IDataStoreProvider"/>) and projects them to catalog entries. An empty query lists
/// everything (browse); a non-empty query filters by name/description (case-insensitive), and
/// <c>EntityTypes</c> filters by kind. Individual containers are not separate catalogue entries — they are
/// chosen within a DataStore when binding a dataset source in the builder, not browsed here.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SearchCatalogEndpoint : Fdw.Services.Catalog.Endpoints.SearchCatalogEndpoint
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly IDataStoreProvider _dataStoreProvider;

    /// <inheritdoc />
    public SearchCatalogEndpoint(DataSetConfigurationProvider dataSetProvider, IDataStoreProvider dataStoreProvider)
    {
        _dataSetProvider = dataSetProvider;
        _dataStoreProvider = dataStoreProvider;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<CatalogEntryDto>> PerformSearch(CatalogSearchRequest req, CancellationToken ct)
    {
        var entries = new List<CatalogEntryDto>();

        var dataSetsResult = await _dataSetProvider.Get(ct).ConfigureAwait(false);
        if (dataSetsResult.IsSuccess && dataSetsResult.Value is not null)
        {
            foreach (var ds in dataSetsResult.Value)
            {
                entries.Add(new CatalogEntryDto
                {
                    EntityType = "DataSet",
                    Name = ds.Name,
                    Description = string.IsNullOrEmpty(ds.Description) ? ds.DisplayName : ds.Description,
                    Owner = ds.ModifyBy,
                    LastModified = ds.ModifyDate.UtcDateTime,
                });
            }
        }

        var storesResult = await _dataStoreProvider.Get(ct).ConfigureAwait(false);
        if (storesResult.IsSuccess && storesResult.Value is not null)
        {
            foreach (var store in storesResult.Value)
            {
                entries.Add(new CatalogEntryDto { EntityType = "DataStore", Name = store.Name });
            }
        }

        return ApplyFilters(entries, req);
    }

    // Why: filtering lives here (in-memory over the enumerated set) — empty query returns all (browse),
    // otherwise case-insensitive contains on name/description; EntityTypes narrows by kind when supplied.
    private static List<CatalogEntryDto> ApplyFilters(List<CatalogEntryDto> entries, CatalogSearchRequest req)
    {
        IEnumerable<CatalogEntryDto> filtered = entries;

        if (req.EntityTypes.Count > 0)
        {
            filtered = filtered.Where(e => req.EntityTypes.Contains(e.EntityType, System.StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(req.Query))
        {
            var q = req.Query;
            filtered = filtered.Where(e =>
                e.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                || (e.Description?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return filtered
            .OrderBy(e => e.EntityType, System.StringComparer.Ordinal)
            .ThenBy(e => e.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
