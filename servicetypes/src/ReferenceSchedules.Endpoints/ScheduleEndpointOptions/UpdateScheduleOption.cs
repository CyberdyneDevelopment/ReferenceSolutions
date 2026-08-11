using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The UpdateSchedule endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "UpdateSchedule")]
public class UpdateScheduleOption : ScheduleEndpointBase<UpdateScheduleEndpoint>
{
}
