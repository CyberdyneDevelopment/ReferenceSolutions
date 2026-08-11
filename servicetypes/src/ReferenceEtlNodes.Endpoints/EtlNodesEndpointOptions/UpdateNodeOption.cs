using Fdw.Collections.Attributes;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>The UpdateNode endpoint.</summary>
[TypeOption(typeof(EtlNodesEndpoints), "UpdateNode")]
public class UpdateNodeOption : EtlNodesEndpointBase<UpdateNodeEndpoint>
{
}
