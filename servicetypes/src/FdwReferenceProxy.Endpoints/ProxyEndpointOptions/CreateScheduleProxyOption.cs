using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateScheduleProxy endpoint.</summary>
[WebMcpTool("create_schedule_proxy", "Create schedule (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "CreateScheduleProxy")]
public class CreateScheduleProxyOption : ProxyEndpointBase<CreateScheduleProxyEndpoint>
{
}
