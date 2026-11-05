using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Tags;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to list dataset annotations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetAnnotationsEndpoint : ListDataSetAnnotationsEndpointBase
{
    /// <inheritdoc />
    public ListDataSetAnnotationsEndpoint(
        IDataSetAnnotationConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        ITagService tags)
        : base(provider, dataSets, tags)
    {
    }
}
