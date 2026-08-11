using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The CreateSchedule endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "CreateSchedule")]
public class CreateScheduleOption : ScheduleEndpointBase<CreateScheduleEndpoint>
{
}
