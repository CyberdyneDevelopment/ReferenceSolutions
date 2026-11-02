using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

[WebMcpTool("get_schedule_wait_thresholds_proxy", "Read the thresholds on waiting runs a schedule states (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetScheduleWaitThresholdsProxy")]
public class GetScheduleWaitThresholdsProxyOption : ProxyEndpointBase<GetScheduleWaitThresholdsProxyEndpoint>
{
}
