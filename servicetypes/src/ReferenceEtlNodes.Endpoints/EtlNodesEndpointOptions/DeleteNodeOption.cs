using Fdw.Collections.Attributes;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>The DeleteNode endpoint.</summary>
[TypeOption(typeof(EtlNodesEndpoints), "DeleteNode")]
public class DeleteNodeOption : EtlNodesEndpointBase<DeleteNodeEndpoint>
{
}
