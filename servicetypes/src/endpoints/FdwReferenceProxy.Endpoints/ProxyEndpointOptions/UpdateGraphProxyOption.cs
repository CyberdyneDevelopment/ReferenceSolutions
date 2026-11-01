using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The UpdateGraphProxy endpoint.</summary>
[WebMcpTool("update_graph_pipeline_proxy", "Replace graph pipeline (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "UpdateGraphProxy")]
public class UpdateGraphProxyOption : ProxyEndpointBase<UpdateGraphProxyEndpoint>
{
}
