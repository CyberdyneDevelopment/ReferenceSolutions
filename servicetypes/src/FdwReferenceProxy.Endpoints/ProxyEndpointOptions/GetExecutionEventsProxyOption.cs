using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetExecutionEventsProxy endpoint.</summary>
[WebMcpTool("get_execution_events_proxy", "Get the events of an execution (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetExecutionEventsProxy")]
public class GetExecutionEventsProxyOption : ProxyEndpointBase<GetExecutionEventsProxyEndpoint>
{
}
