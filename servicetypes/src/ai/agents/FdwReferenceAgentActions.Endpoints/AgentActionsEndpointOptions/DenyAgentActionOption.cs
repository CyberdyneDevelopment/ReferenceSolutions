using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The DenyAgentAction endpoint.</summary>
[WebMcpTool("deny_agent_action", "Deny agent action.")]
[TypeOption(typeof(AgentActionsEndpoints), "DenyAgentAction")]
public class DenyAgentActionOption : AgentActionsEndpointBase<DenyAgentActionEndpoint>
{
}
