using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to list glossary terms one page at a time.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListGlossaryTermsEndpoint : ListGlossaryTermsEndpointBase
{
    /// <inheritdoc />
    public ListGlossaryTermsEndpoint(
        ILogger<ListGlossaryTermsEndpoint> logger,
        IGlossaryTermConfigurationProvider provider,
        ICategoryConfigurationProvider categories,
        IGlossaryTermVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(logger, provider, categories, versions, ancestry, authority, authContext)
    {
    }
}
