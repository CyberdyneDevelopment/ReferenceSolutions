using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Auth;

/// <summary>
/// Deletes an agent key for the current user (DELETE /agent-keys/{keyId}).
/// </summary>
public sealed class DeleteAgentKeyEndpoint : DeleteAgentKeyEndpointBase
{
    private readonly IAgentKeyService _agentKeyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAgentKeyEndpoint"/> class.
    /// </summary>
    public DeleteAgentKeyEndpoint(
        IAgentKeyService agentKeyService,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _agentKeyService = agentKeyService;
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult> DeleteKey(Guid userId, Guid keyId, CancellationToken ct)
        => _agentKeyService.DeleteKey(userId, keyId, ct);
}
