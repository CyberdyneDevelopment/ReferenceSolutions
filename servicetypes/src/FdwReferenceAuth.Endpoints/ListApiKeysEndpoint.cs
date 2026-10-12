using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// List an account's API keys.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListApiKeysEndpoint : ListApiKeysEndpointBase
{
    /// <inheritdoc />
    public ListApiKeysEndpoint(
        UserConfigurationProvider userProvider,
        IPersonalAccessTokenService tokenService,
        IAgentKeyService agentKeyService,
        ILogger<ListApiKeysEndpoint> logger)
        : base(logger, userProvider, tokenService, agentKeyService)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "List an account's API keys";
            s.Description = "User keys and agent keys in one list. Raw keys are never listed.";
        });
        Tags("Users");
    }
}
