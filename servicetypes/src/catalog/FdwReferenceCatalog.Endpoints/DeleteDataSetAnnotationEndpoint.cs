using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to delete a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataSetAnnotationEndpoint : DeleteDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public DeleteDataSetAnnotationEndpoint(IDataSetAnnotationConfigurationProvider provider)
        : base(provider)
    {
    }
}
