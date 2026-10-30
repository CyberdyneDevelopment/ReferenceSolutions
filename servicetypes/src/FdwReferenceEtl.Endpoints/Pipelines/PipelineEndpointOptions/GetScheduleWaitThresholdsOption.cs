using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

[WebMcpTool("get_schedule_wait_thresholds", "Read the thresholds on waiting runs a schedule states.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "GetScheduleWaitThresholds")]
public class GetScheduleWaitThresholdsOption : PipelineEndpointBase<GetScheduleWaitThresholdsEndpoint>
{
}
