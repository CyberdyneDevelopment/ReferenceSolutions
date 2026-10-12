using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to resolve a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResolveDataSetAnnotationEndpoint : ResolveDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public ResolveDataSetAnnotationEndpoint(IDataSetAnnotationConfigurationProvider provider)
        : base(provider)
    {
    }
}
