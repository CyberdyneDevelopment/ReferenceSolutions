using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetExecutionTrackingProxy endpoint.</summary>
[WebMcpTool("get_execution_tracking_proxy", "Get what a run did (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetExecutionTrackingProxy")]
public class GetExecutionTrackingProxyOption : ProxyEndpointBase<GetExecutionTrackingProxyEndpoint>
{
}
