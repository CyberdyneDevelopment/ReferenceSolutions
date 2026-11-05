using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Tags;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to resolve a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResolveDataSetAnnotationEndpoint : ResolveDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public ResolveDataSetAnnotationEndpoint(IDataSetAnnotationConfigurationProvider provider, IDataSetConfigurationProvider dataSets, ITagService tags)
        : base(provider, dataSets, tags)
    {
    }
}
