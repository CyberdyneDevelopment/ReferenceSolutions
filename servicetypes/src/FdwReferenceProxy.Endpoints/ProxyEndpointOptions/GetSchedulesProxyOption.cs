using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetSchedulesProxy endpoint.</summary>
[WebMcpTool("get_schedules_proxy", "List schedules (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetSchedulesProxy")]
public class GetSchedulesProxyOption : ProxyEndpointBase<GetSchedulesProxyEndpoint>
{
}
