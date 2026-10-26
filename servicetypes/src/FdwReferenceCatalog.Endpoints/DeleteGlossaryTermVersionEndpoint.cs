using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to delete a version of a glossary term from an organization or workspace (FDW-2331).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteGlossaryTermVersionEndpoint : DeleteGlossaryTermVersionEndpointBase
{
    /// <inheritdoc />
    public DeleteGlossaryTermVersionEndpoint(
        IGlossaryTermConfigurationProvider terms,
        IGlossaryTermVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(terms, versions, ancestry, authority, authContext)
    {
    }
}
