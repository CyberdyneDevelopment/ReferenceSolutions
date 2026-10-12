using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The DeleteSchedule endpoint.</summary>
[WebMcpTool("delete_schedule", "Delete schedule.")]
[TypeOption(typeof(ScheduleEndpoints), "DeleteSchedule")]
public class DeleteScheduleOption : ScheduleEndpointBase<DeleteScheduleEndpoint>
{
}
