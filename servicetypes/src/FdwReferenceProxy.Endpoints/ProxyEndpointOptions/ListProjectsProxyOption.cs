using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListProjectsProxy endpoint.</summary>
[WebMcpTool("list_projects_proxy", "List ETL projects (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ListProjectsProxy")]
public class ListProjectsProxyOption : ProxyEndpointBase<ListProjectsProxyEndpoint>
{
}
