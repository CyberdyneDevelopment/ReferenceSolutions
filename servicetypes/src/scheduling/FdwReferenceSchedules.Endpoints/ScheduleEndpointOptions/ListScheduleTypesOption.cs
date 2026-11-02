using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The ListScheduleTypes endpoint.</summary>
[WebMcpTool("list_schedule_types", "List schedule types.", ReadOnly = true)]
[TypeOption(typeof(ScheduleEndpoints), "ListScheduleTypes")]
public class ListScheduleTypesOption : ScheduleEndpointBase<ListScheduleTypesEndpoint>
{
}
