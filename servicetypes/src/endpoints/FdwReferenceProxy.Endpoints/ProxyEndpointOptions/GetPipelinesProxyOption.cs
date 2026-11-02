using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetPipelinesProxy endpoint.</summary>
[WebMcpTool("get_pipelines_proxy", "List pipelines (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetPipelinesProxy")]
public class GetPipelinesProxyOption : ProxyEndpointBase<GetPipelinesProxyEndpoint>
{
}
