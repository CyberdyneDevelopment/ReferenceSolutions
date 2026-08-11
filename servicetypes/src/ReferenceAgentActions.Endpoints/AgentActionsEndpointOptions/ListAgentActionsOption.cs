using Fdw.Collections.Attributes;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The ListAgentActions endpoint.</summary>
[TypeOption(typeof(AgentActionsEndpoints), "ListAgentActions")]
public class ListAgentActionsOption : AgentActionsEndpointBase<ListAgentActionsEndpoint>
{
}
