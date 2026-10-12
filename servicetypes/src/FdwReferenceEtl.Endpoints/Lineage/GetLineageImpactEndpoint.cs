using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Lineage;

/// <summary>
/// Concrete endpoint to get downstream lineage impact analysis for an entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetLineageImpactEndpoint : Fdw.Operations.Endpoints.GetLineageImpactEndpointBase
{
    /// <inheritdoc />
    public GetLineageImpactEndpoint(
        LineageConfigurationProvider lineageProvider,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetLineageImpactEndpointBase> logger)
        : base(lineageProvider, pipelineProvider, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Lineage");
    }
}
