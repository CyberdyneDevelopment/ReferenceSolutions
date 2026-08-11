using System.Diagnostics.CodeAnalysis;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.ExternalIdentityProviders.Endpoints;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Concrete <c>GET /auth/external-identity-providers</c> login-discovery endpoint for this host.
/// </summary>
/// <remarks>
/// Returns the public login-discovery subset of the active external identity providers so a UI that
/// cannot open ConfigurationDb gets its login options from the API. Consumed pre-user-login, so the
/// caller is the UI's own service identity holding <c>identityproviders:read</c> — never an end-user
/// token. Anonymous only under DEVELOP (matches the current-tenant endpoint); every other
/// configuration requires the policy.
/// </remarks>
[ExcludeFromCodeCoverage]
public class GetExternalIdentityProvidersEndpoint : GetExternalIdentityProvidersEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetExternalIdentityProvidersEndpoint"/> class.</summary>
    /// <param name="configurationProvider">The gateway-backed external identity provider configuration provider.</param>
    public GetExternalIdentityProvidersEndpoint(ExternalIdentityProviderConfigurationProvider configurationProvider)
        : base(configurationProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
#if DEVELOP
        AllowAnonymous();
#else
        Policies("identityproviders:read");
#endif
        Tags("Authentication");
    }
}
