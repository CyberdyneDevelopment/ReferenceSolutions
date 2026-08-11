using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The GetSchedule endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "GetSchedule")]
public class GetScheduleOption : ScheduleEndpointBase<GetScheduleEndpoint>
{
}
