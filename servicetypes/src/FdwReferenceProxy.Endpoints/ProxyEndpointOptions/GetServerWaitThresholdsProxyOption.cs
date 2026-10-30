using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

[WebMcpTool("get_server_wait_thresholds_proxy", "Read the thresholds on waiting runs the server states (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetServerWaitThresholdsProxy")]
public class GetServerWaitThresholdsProxyOption : ProxyEndpointBase<GetServerWaitThresholdsProxyEndpoint>
{
}
