using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The ListPipelineHistoryProxy endpoint.</summary>
[WebMcpTool("list_pipeline_history_proxy", "List pipeline run history (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "ListPipelineHistoryProxy")]
public class ListPipelineHistoryProxyOption : ProxyEndpointBase<ListPipelineHistoryProxyEndpoint>
{
}
