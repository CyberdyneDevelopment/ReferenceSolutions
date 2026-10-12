using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetScheduleTypesProxy endpoint.</summary>
[WebMcpTool("get_schedule_types_proxy", "List schedule types (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetScheduleTypesProxy")]
public class GetScheduleTypesProxyOption : ProxyEndpointBase<GetScheduleTypesProxyEndpoint>
{
}
