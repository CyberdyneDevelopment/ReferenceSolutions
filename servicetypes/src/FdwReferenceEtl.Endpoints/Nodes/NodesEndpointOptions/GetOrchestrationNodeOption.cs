using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Nodes.NodesEndpointOptions;

/// <summary>The GetNode endpoint.</summary>
[WebMcpTool("get_node", "Get an orchestration node", ReadOnly = true)]
[TypeOption(typeof(NodesEndpoints), "GetNode")]
public class GetOrchestrationNodeOption : NodesEndpointBase<GetOrchestrationNodeEndpoint>
{
}
