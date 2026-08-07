using Fdw.Operations.Endpoints.AgentActions;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.AgentActions;

/// <summary>
/// Lists agent actions with optional status filter.
/// </summary>
public sealed class ListAgentActionsEndpoint : ListAgentActionsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAgentActionsEndpoint"/> class.
    /// </summary>
    public ListAgentActionsEndpoint(IAgentActionService agentActionService, ILogger<ListAgentActionsEndpointBase>? logger)
        : base(agentActionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("AgentActions");
}
