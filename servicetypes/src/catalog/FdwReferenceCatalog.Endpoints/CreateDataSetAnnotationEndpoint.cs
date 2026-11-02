using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to create a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataSetAnnotationEndpoint : CreateDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public CreateDataSetAnnotationEndpoint(IDataSetAnnotationConfigurationProvider provider)
        : base(provider)
    {
    }
}
