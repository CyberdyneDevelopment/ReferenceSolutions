using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetProjectProxy endpoint.</summary>
[WebMcpTool("get_project_proxy", "Get an ETL project by ID (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetProjectProxy")]
public class GetProjectProxyOption : ProxyEndpointBase<GetProjectProxyEndpoint>
{
}
