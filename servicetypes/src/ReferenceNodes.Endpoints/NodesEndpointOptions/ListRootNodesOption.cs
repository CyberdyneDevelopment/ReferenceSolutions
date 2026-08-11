using Fdw.Collections.Attributes;

namespace ReferenceNodes.Endpoints.NodesEndpointOptions;

/// <summary>The ListRootNodes endpoint.</summary>
[TypeOption(typeof(NodesEndpoints), "ListRootNodes")]
public class ListRootNodesOption : NodesEndpointBase<ListRootNodesEndpoint>
{
}
