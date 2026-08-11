using Fdw.Collections.Attributes;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>The GetNode endpoint.</summary>
[TypeOption(typeof(EtlNodesEndpoints), "GetNode")]
public class GetNodeOption : EtlNodesEndpointBase<GetNodeEndpoint>
{
}
