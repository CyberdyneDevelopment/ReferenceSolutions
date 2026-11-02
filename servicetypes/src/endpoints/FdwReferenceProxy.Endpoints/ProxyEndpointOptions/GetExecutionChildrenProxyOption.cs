using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetExecutionChildrenProxy endpoint.</summary>
[WebMcpTool("get_execution_children_proxy", "Get the child executions of an execution (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetExecutionChildrenProxy")]
public class GetExecutionChildrenProxyOption : ProxyEndpointBase<GetExecutionChildrenProxyEndpoint>
{
}
