using Fdw.Collections.Attributes;

namespace ReferenceAgentActions.Endpoints.AgentActionsEndpointOptions;

/// <summary>The ApproveAgentAction endpoint.</summary>
[TypeOption(typeof(AgentActionsEndpoints), "ApproveAgentAction")]
public class ApproveAgentActionOption : AgentActionsEndpointBase<ApproveAgentActionEndpoint>
{
}
