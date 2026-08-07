using Fdw.Operations.Endpoints.AgentActions;
using Fdw.Services.Agents.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.AgentActions;

/// <summary>
/// Gets a single agent action by ID.
/// </summary>
public sealed class GetAgentActionEndpoint : GetAgentActionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAgentActionEndpoint"/> class.
    /// </summary>
    public GetAgentActionEndpoint(IAgentActionService agentActionService, ILogger<GetAgentActionEndpointBase>? logger)
        : base(agentActionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("AgentActions");
}
