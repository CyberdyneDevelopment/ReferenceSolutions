using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetPipelineHistoryProxy endpoint.</summary>
[WebMcpTool("get_pipeline_history_proxy", "Get the run history of one pipeline (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetPipelineHistoryProxy")]
public class GetPipelineHistoryProxyOption : ProxyEndpointBase<GetPipelineHistoryProxyEndpoint>
{
}
