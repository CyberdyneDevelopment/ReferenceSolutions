using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to delete a dataset's annotation by the name of the dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataSetAnnotationByDataSetEndpoint : DeleteDataSetAnnotationByDataSetEndpointBase
{
    /// <inheritdoc />
    public DeleteDataSetAnnotationByDataSetEndpoint(IDataSetAnnotationConfigurationProvider provider)
        : base(provider)
    {
    }
}
