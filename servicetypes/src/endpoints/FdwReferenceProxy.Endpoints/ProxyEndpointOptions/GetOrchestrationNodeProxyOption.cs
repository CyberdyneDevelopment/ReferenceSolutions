using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetOrchestrationNodeProxy endpoint.</summary>
[WebMcpTool("get_orchestration_node_proxy", "Get an orchestration node (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetOrchestrationNodeProxy")]
public class GetOrchestrationNodeProxyOption : ProxyEndpointBase<GetOrchestrationNodeProxyEndpoint>
{
}
