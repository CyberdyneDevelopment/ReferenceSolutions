using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Tags;
using Fdw.Services.Authentication.Abstractions.Security;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to update a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataSetAnnotationEndpoint : UpdateDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public UpdateDataSetAnnotationEndpoint(
        IDataSetAnnotationConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        ITagService tags,
        IAuthenticationContextAccessor authentication)
        : base(provider, dataSets, tags, authentication)
    {
    }
}
