using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The TriggerOrchestrationNodeProxy endpoint.</summary>
[WebMcpTool("trigger_orchestration_node_proxy", "Trigger an orchestration node (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "TriggerOrchestrationNodeProxy")]
public class TriggerOrchestrationNodeProxyOption : ProxyEndpointBase<TriggerOrchestrationNodeProxyEndpoint>
{
}
