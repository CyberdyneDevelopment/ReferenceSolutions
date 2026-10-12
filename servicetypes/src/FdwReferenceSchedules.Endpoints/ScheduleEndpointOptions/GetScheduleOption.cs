using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The GetSchedule endpoint.</summary>
[WebMcpTool("get_schedule", "Get schedule.", ReadOnly = true)]
[TypeOption(typeof(ScheduleEndpoints), "GetSchedule")]
public class GetScheduleOption : ScheduleEndpointBase<GetScheduleEndpoint>
{
}
