using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListPipelineTemplatesProxy endpoint.</summary>
[WebMcpTool("list_pipeline_templates_proxy", "List pipeline templates (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ListPipelineTemplatesProxy")]
public class ListPipelineTemplatesProxyOption : ProxyEndpointBase<ListPipelineTemplatesProxyEndpoint>
{
}
