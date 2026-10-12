using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetScheduleProxy endpoint.</summary>
[WebMcpTool("get_schedule_proxy", "Get schedule (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetScheduleProxy")]
public class GetScheduleProxyOption : ProxyEndpointBase<GetScheduleProxyEndpoint>
{
}
