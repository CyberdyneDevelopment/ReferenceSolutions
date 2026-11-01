using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdateScheduleProxy endpoint.</summary>
[WebMcpTool("update_schedule_proxy", "Update schedule (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "UpdateScheduleProxy")]
public class UpdateScheduleProxyOption : ProxyEndpointBase<UpdateScheduleProxyEndpoint>
{
}
