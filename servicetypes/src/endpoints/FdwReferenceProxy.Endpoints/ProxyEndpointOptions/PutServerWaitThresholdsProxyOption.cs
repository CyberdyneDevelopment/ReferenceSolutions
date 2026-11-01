using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

[WebMcpTool("put_server_wait_thresholds_proxy", "State the thresholds on waiting runs the server states (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "PutServerWaitThresholdsProxy")]
public class PutServerWaitThresholdsProxyOption : ProxyEndpointBase<PutServerWaitThresholdsProxyEndpoint>
{
}
