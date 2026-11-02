using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

[WebMcpTool("put_server_wait_thresholds", "State the thresholds on waiting runs the server states.")]
[TypeOption(typeof(PipelineEndpoints), "PutServerWaitThresholds")]
public class PutServerWaitThresholdsOption : PipelineEndpointBase<PutServerWaitThresholdsEndpoint>
{
}
