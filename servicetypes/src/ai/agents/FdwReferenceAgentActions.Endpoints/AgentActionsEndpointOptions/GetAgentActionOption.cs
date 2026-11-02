using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The GetAgentAction endpoint.</summary>
[WebMcpTool("get_agent_action", "Get agent action.", ReadOnly = true)]
[TypeOption(typeof(AgentActionsEndpoints), "GetAgentAction")]
public class GetAgentActionOption : AgentActionsEndpointBase<GetAgentActionEndpoint>
{
}
