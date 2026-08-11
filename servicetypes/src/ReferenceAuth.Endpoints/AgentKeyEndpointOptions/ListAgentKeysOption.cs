using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AgentKeyEndpointOptions;

/// <summary>The ListAgentKeys endpoint.</summary>
[TypeOption(typeof(AgentKeyEndpoints), "ListAgentKeys")]
public class ListAgentKeysOption : AgentKeyEndpointBase<ListAgentKeysEndpoint>
{
}
