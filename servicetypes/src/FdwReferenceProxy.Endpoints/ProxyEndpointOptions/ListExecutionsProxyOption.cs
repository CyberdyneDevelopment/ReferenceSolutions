using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListExecutionsProxy endpoint.</summary>
[WebMcpTool("list_executions_proxy", "List executions (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ListExecutionsProxy")]
public class ListExecutionsProxyOption : ProxyEndpointBase<ListExecutionsProxyEndpoint>
{
}
