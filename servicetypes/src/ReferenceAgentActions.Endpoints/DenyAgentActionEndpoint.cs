using Fdw.Operations.Endpoints.AgentActions;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceAgentActions.Endpoints;

/// <summary>
/// Denies a pending agent action.
/// </summary>
public sealed class DenyAgentActionEndpoint : DenyAgentActionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenyAgentActionEndpoint"/> class.
    /// </summary>
    public DenyAgentActionEndpoint(IAgentActionService agentActionService, ILogger<DenyAgentActionEndpointBase>? logger)
        : base(agentActionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("AgentActions");
}
