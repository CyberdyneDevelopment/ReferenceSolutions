using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

[WebMcpTool("get_pipeline_wait_thresholds", "Read the thresholds on waiting runs a pipeline states.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "GetPipelineWaitThresholds")]
public class GetPipelineWaitThresholdsOption : PipelineEndpointBase<GetPipelineWaitThresholdsEndpoint>
{
}
