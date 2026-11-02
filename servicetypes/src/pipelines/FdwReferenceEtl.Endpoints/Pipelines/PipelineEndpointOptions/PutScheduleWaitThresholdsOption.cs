using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

[WebMcpTool("put_schedule_wait_thresholds", "State the thresholds on waiting runs a schedule states.")]
[TypeOption(typeof(PipelineEndpoints), "PutScheduleWaitThresholds")]
public class PutScheduleWaitThresholdsOption : PipelineEndpointBase<PutScheduleWaitThresholdsEndpoint>
{
}
