using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ToggleScheduleProxy endpoint.</summary>
[WebMcpTool("toggle_schedule_proxy", "Toggle schedule (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "ToggleScheduleProxy")]
public class ToggleScheduleProxyOption : ProxyEndpointBase<ToggleScheduleProxyEndpoint>
{
}
