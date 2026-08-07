using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;

namespace Reference.Api.Endpoints;

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

/// <summary>
/// Concrete endpoint to list glossary terms.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListGlossaryTermsEndpoint : Fdw.Services.Catalog.Endpoints.ListGlossaryTermsEndpoint
{
    /// <inheritdoc />
    public ListGlossaryTermsEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to get a glossary term by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.GetGlossaryTermEndpoint
{
    /// <inheritdoc />
    public GetGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to create a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.CreateGlossaryTermEndpoint
{
    /// <inheritdoc />
    public CreateGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to update a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.UpdateGlossaryTermEndpoint
{
    /// <inheritdoc />
    public UpdateGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to delete a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.DeleteGlossaryTermEndpoint
{
    /// <inheritdoc />
    public DeleteGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to get a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.GetDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public GetDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to update a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.UpdateDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public UpdateDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to list dataset annotations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetAnnotationsEndpoint : Fdw.Services.Catalog.Endpoints.ListDataSetAnnotationsEndpoint
{
    /// <inheritdoc />
    public ListDataSetAnnotationsEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to create a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.CreateDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public CreateDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to delete a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.DeleteDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public DeleteDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}

/// <summary>
/// Concrete endpoint to resolve a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResolveDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.ResolveDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public ResolveDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
