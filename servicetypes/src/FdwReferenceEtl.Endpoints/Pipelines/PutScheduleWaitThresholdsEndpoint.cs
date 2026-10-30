using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

[ExcludeFromCodeCoverage]
public class PutScheduleWaitThresholdsEndpoint : PutWaitThresholdLevelEndpointBase
{
    public PutScheduleWaitThresholdsEndpoint(IWaitThresholdStore store)
        : base(store)
    {
    }

    protected override string OwnerKind => WaitThresholdOwners.Schedule;

    protected override string RoutePath => "/etl/wait-thresholds/schedules/{name}";

    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
