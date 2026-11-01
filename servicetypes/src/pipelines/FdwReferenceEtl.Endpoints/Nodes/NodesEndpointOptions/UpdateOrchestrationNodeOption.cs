using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Nodes.NodesEndpointOptions;

/// <summary>The UpdateNode endpoint.</summary>
[WebMcpTool("update_node", "Update an orchestration node")]
[TypeOption(typeof(NodesEndpoints), "UpdateNode")]
public class UpdateOrchestrationNodeOption : NodesEndpointBase<UpdateOrchestrationNodeEndpoint>
{
}
