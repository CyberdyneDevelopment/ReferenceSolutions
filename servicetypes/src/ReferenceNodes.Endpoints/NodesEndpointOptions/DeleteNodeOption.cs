using Fdw.Collections.Attributes;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The DeleteNode endpoint.</summary>
[TypeOption(typeof(NodesEndpoints), "DeleteNode")]
public class DeleteNodeOption : NodesEndpointBase<DeleteNodeEndpoint>
{
}
