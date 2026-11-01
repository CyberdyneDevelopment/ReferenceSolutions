using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// List an account's sign-in methods.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetSignInMethodsEndpoint : GetSignInMethodsEndpointBase
{
    /// <inheritdoc />
    public GetSignInMethodsEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        IExternalIdentityConfigurationProvider externalIdentities,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService,
        ILogger<GetSignInMethodsEndpoint> logger)
        : base(logger, userProvider, credentialService, externalIdentities, tokenService, agentKeyService)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "List an account's sign-in methods";
            s.Description = "Password state, external-identity bindings and API keys, with what the calling token may do about each.";
        });
        Tags("Users");
    }
}
