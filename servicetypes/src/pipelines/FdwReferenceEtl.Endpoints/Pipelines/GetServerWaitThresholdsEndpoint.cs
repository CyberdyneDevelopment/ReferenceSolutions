using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

[ExcludeFromCodeCoverage]
public class GetServerWaitThresholdsEndpoint : GetWaitThresholdLevelEndpointBase
{
    public GetServerWaitThresholdsEndpoint(IWaitThresholdStore store)
        : base(store)
    {
    }

    protected override string OwnerKind => WaitThresholdOwners.Server;

    protected override string RoutePath => "/etl/wait-thresholds/server";

    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
