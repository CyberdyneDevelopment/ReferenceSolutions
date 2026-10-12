using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Revoke an API key.
/// </summary>
[ExcludeFromCodeCoverage]
public class RevokeApiKeyEndpoint : RevokeApiKeyEndpointBase
{
    /// <inheritdoc />
    public RevokeApiKeyEndpoint(
        UserConfigurationProvider userProvider,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService,
        ILogger<RevokeApiKeyEndpoint> logger)
        : base(logger, userProvider, tokenService, agentKeyService)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Revoke an API key";
            s.Description = "Revokes a user or agent key.";
        });
        Tags("Users");
    }
}
