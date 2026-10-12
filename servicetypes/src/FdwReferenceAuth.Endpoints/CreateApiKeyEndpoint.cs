using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Mint an API key.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateApiKeyEndpoint : CreateApiKeyEndpointBase
{
    /// <inheritdoc />
    public CreateApiKeyEndpoint(
        UserConfigurationProvider userProvider,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService,
        ILogger<CreateApiKeyEndpoint> logger)
        : base(logger, userProvider, tokenService, agentKeyService)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Mint an API key";
            s.Description = "Mints a user or agent key. The raw key is in this response and nowhere else.";
        });
        Tags("Users");
    }
}
