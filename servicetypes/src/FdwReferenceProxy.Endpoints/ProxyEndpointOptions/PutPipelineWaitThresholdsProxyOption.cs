using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceProxy.Endpoints.ProxyEndpointOptions;

[WebMcpTool("put_pipeline_wait_thresholds_proxy", "State the thresholds on waiting runs a pipeline states (proxy)")]
[TypeOption(typeof(ProxyEndpoints), "PutPipelineWaitThresholdsProxy")]
public class PutPipelineWaitThresholdsProxyOption : ProxyEndpointBase<PutPipelineWaitThresholdsProxyEndpoint>
{
}
