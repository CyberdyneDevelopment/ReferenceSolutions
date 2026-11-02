using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The CreateSchedule endpoint.</summary>
[WebMcpTool("create_schedule", "Create schedule.")]
[TypeOption(typeof(ScheduleEndpoints), "CreateSchedule")]
public class CreateScheduleOption : ScheduleEndpointBase<CreateScheduleEndpoint>
{
}
