using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateGraphProxy endpoint.</summary>
[WebMcpTool("create_graph_pipeline_proxy", "Create graph pipeline (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "CreateGraphProxy")]
public class CreateGraphProxyOption : ProxyEndpointBase<CreateGraphProxyEndpoint>
{
}
