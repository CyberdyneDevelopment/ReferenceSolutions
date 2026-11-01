using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The ApproveAgentAction endpoint.</summary>
[WebMcpTool("approve_agent_action", "Approve agent action.")]
[TypeOption(typeof(AgentActionsEndpoints), "ApproveAgentAction")]
public class ApproveAgentActionOption : AgentActionsEndpointBase<ApproveAgentActionEndpoint>
{
}
