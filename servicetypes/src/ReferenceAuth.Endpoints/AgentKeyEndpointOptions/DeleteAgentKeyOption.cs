using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AgentKeyEndpointOptions;

/// <summary>The DeleteAgentKey endpoint.</summary>
[TypeOption(typeof(AgentKeyEndpoints), "DeleteAgentKey")]
public class DeleteAgentKeyOption : AgentKeyEndpointBase<DeleteAgentKeyEndpoint>
{
}
