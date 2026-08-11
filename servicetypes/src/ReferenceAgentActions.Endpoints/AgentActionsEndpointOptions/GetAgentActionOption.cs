using Fdw.Collections.Attributes;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The GetAgentAction endpoint.</summary>
[TypeOption(typeof(AgentActionsEndpoints), "GetAgentAction")]
public class GetAgentActionOption : AgentActionsEndpointBase<GetAgentActionEndpoint>
{
}
