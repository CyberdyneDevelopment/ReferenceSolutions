using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetGraphProxy endpoint.</summary>
[WebMcpTool("get_graph_pipeline_proxy", "Get graph pipeline (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetGraphProxy")]
public class GetGraphProxyOption : ProxyEndpointBase<GetGraphProxyEndpoint>
{
}
