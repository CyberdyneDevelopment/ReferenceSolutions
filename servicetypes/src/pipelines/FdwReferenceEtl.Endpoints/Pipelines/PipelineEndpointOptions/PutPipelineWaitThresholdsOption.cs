using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

[WebMcpTool("put_pipeline_wait_thresholds", "State the thresholds on waiting runs a pipeline states.")]
[TypeOption(typeof(PipelineEndpoints), "PutPipelineWaitThresholds")]
public class PutPipelineWaitThresholdsOption : PipelineEndpointBase<PutPipelineWaitThresholdsEndpoint>
{
}
