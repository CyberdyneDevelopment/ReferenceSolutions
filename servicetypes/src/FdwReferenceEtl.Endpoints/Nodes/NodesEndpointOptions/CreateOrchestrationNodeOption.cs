using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Nodes.NodesEndpointOptions;

/// <summary>The CreateNode endpoint.</summary>
[WebMcpTool("create_node", "Create an orchestration node")]
[TypeOption(typeof(NodesEndpoints), "CreateNode")]
public class CreateOrchestrationNodeOption : NodesEndpointBase<CreateOrchestrationNodeEndpoint>
{
}
