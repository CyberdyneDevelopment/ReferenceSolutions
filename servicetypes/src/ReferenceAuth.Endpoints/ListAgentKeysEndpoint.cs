using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Lists the current user's agent keys (GET /agent-keys).
/// </summary>
public class ListAgentKeysEndpoint : ListAgentKeysEndpointBase
{
    private readonly IAgentKeyService _agentKeyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAgentKeysEndpoint"/> class.
    /// </summary>
    public ListAgentKeysEndpoint(
        IAgentKeyService agentKeyService,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _agentKeyService = agentKeyService;
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<IReadOnlyList<AgentKeySummary>>> ListKeys(
        Guid userId, CancellationToken ct)
        => _agentKeyService.ListKeys(userId, ct);
}
