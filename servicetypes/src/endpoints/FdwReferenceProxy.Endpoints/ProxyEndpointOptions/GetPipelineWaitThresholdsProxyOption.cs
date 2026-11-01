using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

[WebMcpTool("get_pipeline_wait_thresholds_proxy", "Read the thresholds on waiting runs a pipeline states (proxy)", ReadOnly = true)]
[TypeOption(typeof(ProxyEndpoints), "GetPipelineWaitThresholdsProxy")]
public class GetPipelineWaitThresholdsProxyOption : ProxyEndpointBase<GetPipelineWaitThresholdsProxyEndpoint>
{
}
