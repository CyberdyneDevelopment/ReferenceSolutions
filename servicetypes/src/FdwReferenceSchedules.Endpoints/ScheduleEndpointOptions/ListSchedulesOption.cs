using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The ListSchedules endpoint.</summary>
[WebMcpTool("list_schedules", "List schedules.", ReadOnly = true)]
[TypeOption(typeof(ScheduleEndpoints), "ListSchedules")]
public class ListSchedulesOption : ScheduleEndpointBase<ListSchedulesEndpoint>
{
}
