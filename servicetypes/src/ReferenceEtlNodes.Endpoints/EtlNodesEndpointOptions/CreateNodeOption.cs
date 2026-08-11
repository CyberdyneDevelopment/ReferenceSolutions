using Fdw.Collections.Attributes;

namespace ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

/// <summary>The CreateNode endpoint.</summary>
[TypeOption(typeof(EtlNodesEndpoints), "CreateNode")]
public class CreateNodeOption : EtlNodesEndpointBase<CreateNodeEndpoint>
{
}
