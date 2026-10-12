using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Nodes.NodesEndpointOptions;

/// <summary>The ListRootNodes endpoint.</summary>
[WebMcpTool("list_root_nodes", "List root orchestration nodes", ReadOnly = true)]
[TypeOption(typeof(NodesEndpoints), "ListRootNodes")]
public class ListRootOrchestrationNodesOption : NodesEndpointBase<ListRootOrchestrationNodesEndpoint>
{
}
