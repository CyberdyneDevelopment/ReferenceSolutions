using Fdw.Operations.Endpoints.AgentActions;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.AgentActions;

/// <summary>
/// Approves a pending agent action.
/// </summary>
public sealed class ApproveAgentActionEndpoint : ApproveAgentActionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAgentActionEndpoint"/> class.
    /// </summary>
    public ApproveAgentActionEndpoint(IAgentActionService agentActionService, ILogger<ApproveAgentActionEndpointBase>? logger)
        : base(agentActionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("AgentActions");
}
