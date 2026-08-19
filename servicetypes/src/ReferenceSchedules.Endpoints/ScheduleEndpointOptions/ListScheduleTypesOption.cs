using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The ListScheduleTypes endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "ListScheduleTypes")]
public class ListScheduleTypesOption : ScheduleEndpointBase<ListScheduleTypesEndpoint>
{
}
