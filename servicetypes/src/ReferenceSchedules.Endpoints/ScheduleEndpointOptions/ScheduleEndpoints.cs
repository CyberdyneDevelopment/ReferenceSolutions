using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSchedules.Endpoints.ScheduleEndpointOptions;

/// <summary>The endpoints over the schedule resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ScheduleEndpointBase), typeof(IEndpointTypeOption), typeof(ScheduleEndpoints))]
public partial class ScheduleEndpoints : EndpointTypeCollectionBase<ScheduleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
