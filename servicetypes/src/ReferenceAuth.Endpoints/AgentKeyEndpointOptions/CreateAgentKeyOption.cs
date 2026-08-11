using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AgentKeyEndpointOptions;

/// <summary>The CreateAgentKey endpoint.</summary>
[TypeOption(typeof(AgentKeyEndpoints), "CreateAgentKey")]
public class CreateAgentKeyOption : AgentKeyEndpointBase<CreateAgentKeyEndpoint>
{
}
