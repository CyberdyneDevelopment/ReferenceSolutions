using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The UpdateSchedule endpoint.</summary>
[WebMcpTool("update_schedule", "Update schedule.")]
[TypeOption(typeof(ScheduleEndpoints), "UpdateSchedule")]
public class UpdateScheduleOption : ScheduleEndpointBase<UpdateScheduleEndpoint>
{
}
