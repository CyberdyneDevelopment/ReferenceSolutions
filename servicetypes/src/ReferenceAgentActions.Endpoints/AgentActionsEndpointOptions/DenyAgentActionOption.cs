using Fdw.Collections.Attributes;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The DenyAgentAction endpoint.</summary>
[TypeOption(typeof(AgentActionsEndpoints), "DenyAgentAction")]
public class DenyAgentActionOption : AgentActionsEndpointBase<DenyAgentActionEndpoint>
{
}
