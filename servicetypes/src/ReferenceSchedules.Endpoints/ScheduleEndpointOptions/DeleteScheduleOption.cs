using Fdw.Collections.Attributes;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The DeleteSchedule endpoint.</summary>
[TypeOption(typeof(ScheduleEndpoints), "DeleteSchedule")]
public class DeleteScheduleOption : ScheduleEndpointBase<DeleteScheduleEndpoint>
{
}
