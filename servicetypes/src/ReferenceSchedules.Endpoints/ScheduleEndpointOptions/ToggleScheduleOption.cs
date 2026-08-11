using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The ToggleSchedule endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "ToggleSchedule")]
public class ToggleScheduleOption : ScheduleEndpointBase<ToggleScheduleEndpoint>
{
}
