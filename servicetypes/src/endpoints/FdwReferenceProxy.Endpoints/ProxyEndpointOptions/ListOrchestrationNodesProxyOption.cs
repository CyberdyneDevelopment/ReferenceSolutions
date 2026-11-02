using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListOrchestrationNodesProxy endpoint.</summary>
[WebMcpTool("list_orchestration_nodes_proxy", "List root orchestration nodes (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ListOrchestrationNodesProxy")]
public class ListOrchestrationNodesProxyOption : ProxyEndpointBase<ListOrchestrationNodesProxyEndpoint>
{
}
