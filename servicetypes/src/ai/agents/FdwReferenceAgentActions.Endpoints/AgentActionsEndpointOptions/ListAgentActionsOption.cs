using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The ListAgentActions endpoint.</summary>
[WebMcpTool("list_agent_actions", "List agent actions.", ReadOnly = true)]
[TypeOption(typeof(AgentActionsEndpoints), "ListAgentActions")]
public class ListAgentActionsOption : AgentActionsEndpointBase<ListAgentActionsEndpoint>
{
}
