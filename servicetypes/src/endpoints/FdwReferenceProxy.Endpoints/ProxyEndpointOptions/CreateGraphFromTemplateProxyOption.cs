using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The CreateGraphFromTemplateProxy endpoint.</summary>
[WebMcpTool("create_graph_pipeline_from_template_proxy", "Make a pipeline from a template (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "CreateGraphFromTemplateProxy")]
public class CreateGraphFromTemplateProxyOption : ProxyEndpointBase<CreateGraphFromTemplateProxyEndpoint>
{
}
