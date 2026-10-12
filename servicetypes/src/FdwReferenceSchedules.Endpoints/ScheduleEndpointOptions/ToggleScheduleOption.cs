using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The ToggleSchedule endpoint.</summary>
[WebMcpTool("toggle_schedule", "Toggle schedule.")]
[TypeOption(typeof(ScheduleEndpoints), "ToggleSchedule")]
public class ToggleScheduleOption : ScheduleEndpointBase<ToggleScheduleEndpoint>
{
}
