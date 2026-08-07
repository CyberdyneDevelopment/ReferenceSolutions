using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Auth;

/// <summary>
/// Creates an agent key for the current user (POST /agent-keys).
/// </summary>
public sealed class CreateAgentKeyEndpoint : CreateAgentKeyEndpointBase
{
    private readonly IAgentKeyService _agentKeyService;
    private readonly UserConfigurationProvider _userProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAgentKeyEndpoint"/> class.
    /// </summary>
    public CreateAgentKeyEndpoint(
        IAgentKeyService agentKeyService,
        UserConfigurationProvider userProvider,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _agentKeyService = agentKeyService;
        _userProvider = userProvider;
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<AgentKeyCreatedResult>> CreateKey(
        Guid userId, string label, DateTime? expiresAt, CancellationToken ct)
    {
        // Why: agent.AgentKey stores the owning user's display name for audit/diagnostics.
        // Resolve it from the authenticated user; fail loud if the user can't be resolved.
        var userResult = await _userProvider.GetUser(userId, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
            return userResult.ToNewResult<AgentKeyCreatedResult>();
        var user = userResult.Value;

        return await _agentKeyService
            .CreateKey(userId, user.Username, label, expiresAt, ct)
            .ConfigureAwait(false);
    }
}
