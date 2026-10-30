using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

[WebMcpTool("put_schedule_wait_thresholds_proxy", "State the thresholds on waiting runs a schedule states (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "PutScheduleWaitThresholdsProxy")]
public class PutScheduleWaitThresholdsProxyOption : ProxyEndpointBase<PutScheduleWaitThresholdsProxyEndpoint>
{
}
