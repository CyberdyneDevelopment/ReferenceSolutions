using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The GetPipelineDetailProxy endpoint.</summary>
[WebMcpTool("get_pipeline_detail_proxy", "Get a pipeline (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetPipelineDetailProxy")]
public class GetPipelineDetailProxyOption : ProxyEndpointBase<GetPipelineDetailProxyEndpoint>
{
}
