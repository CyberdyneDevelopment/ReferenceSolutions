using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to list the versions of a glossary term at the places the caller may read (FDW-2331).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListGlossaryTermVersionsEndpoint : ListGlossaryTermVersionsEndpointBase
{
    /// <inheritdoc />
    public ListGlossaryTermVersionsEndpoint(
        IGlossaryTermConfigurationProvider terms,
        IGlossaryTermVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext,
        IPlaceNames names)
        : base(terms, versions, ancestry, authority, authContext, names)
    {
    }
}
