using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

[ExcludeFromCodeCoverage]
public class PutPipelineWaitThresholdsEndpoint : PutWaitThresholdLevelEndpointBase
{
    public PutPipelineWaitThresholdsEndpoint(IWaitThresholdStore store)
        : base(store)
    {
    }

    protected override string OwnerKind => WaitThresholdOwners.Pipeline;

    protected override string RoutePath => "/etl/wait-thresholds/pipelines/{name}";

    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
