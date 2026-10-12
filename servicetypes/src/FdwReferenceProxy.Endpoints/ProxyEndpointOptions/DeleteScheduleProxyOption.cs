using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The DeleteScheduleProxy endpoint.</summary>
[WebMcpTool("delete_schedule_proxy", "Delete schedule (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "DeleteScheduleProxy")]
public class DeleteScheduleProxyOption : ProxyEndpointBase<DeleteScheduleProxyEndpoint>
{
}
