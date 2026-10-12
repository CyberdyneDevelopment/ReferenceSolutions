using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Nodes.NodesEndpointOptions;

/// <summary>The DeleteNode endpoint.</summary>
[WebMcpTool("delete_node", "Delete an orchestration node")]
[TypeOption(typeof(NodesEndpoints), "DeleteNode")]
public class DeleteOrchestrationNodeOption : NodesEndpointBase<DeleteOrchestrationNodeEndpoint>
{
}
