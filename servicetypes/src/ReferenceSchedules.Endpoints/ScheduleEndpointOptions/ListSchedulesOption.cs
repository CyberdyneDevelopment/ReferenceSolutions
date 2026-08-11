using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The ListSchedules endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "ListSchedules")]
public class ListSchedulesOption : ScheduleEndpointBase<ListSchedulesEndpoint>
{
}
