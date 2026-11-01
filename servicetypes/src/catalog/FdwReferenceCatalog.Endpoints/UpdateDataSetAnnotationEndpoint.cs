using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to update a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataSetAnnotationEndpoint : UpdateDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public UpdateDataSetAnnotationEndpoint(IDataSetAnnotationConfigurationProvider provider)
        : base(provider)
    {
    }
}
