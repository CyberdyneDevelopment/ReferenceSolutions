using ReferenceAuthentication.OpenIddict.Endpoints;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Concrete <c>POST /connect/token</c> OpenIddict token endpoint for this host.
/// </summary>
/// <remarks>
/// The abstract <see cref="ConnectTokenEndpointBase"/> ships in FDW with all grant-type routing logic
/// (password / client_credentials / refresh_token / authorization_code) but registers no route unless a
/// concrete subclass exists in the host app's assembly — FastEndpoints only discovers endpoints in the
/// entry assembly, not referenced package assemblies. Without this class the OpenIddict token endpoint
/// 404s after OpenIddict validates the request. Mirrors the same pattern as <c>LogoutEndpoint</c>.
/// </remarks>
public class ConnectTokenEndpoint : ConnectTokenEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectTokenEndpoint"/> class.
    /// </summary>
    /// <param name="authenticationService">The generic, provider-agnostic authN service.</param>
    /// <param name="externalIdentityProviderResolver">Resolves the external identity provider for the external_identity grant.</param>
    /// <param name="logger">The optional logger.</param>
    public ConnectTokenEndpoint(
        IAuthenticationService authenticationService,
        ExternalIdentityProviderResolver externalIdentityProviderResolver,
        ILogger<ConnectTokenEndpointBase>? logger)
        : base(authenticationService, externalIdentityProviderResolver, logger)
    {
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        base.Configure();
        // Why: this app sets a global FastEndpoints RoutePrefix ("api/v1"), which would map the token
        // endpoint at /api/v1/connect/token. OpenIddict's server pipeline (SetTokenEndpointUris) hardcodes
        // /connect/token and pass-through routes there — so the FE endpoint MUST sit at the un-prefixed
        // /connect/token. Clear the prefix for this endpoint only.
        RoutePrefixOverride(string.Empty);
    }
}
