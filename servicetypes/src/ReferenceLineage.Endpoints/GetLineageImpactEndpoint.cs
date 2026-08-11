using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceLineage.Endpoints;

/// <summary>
/// Concrete endpoint to get downstream lineage impact analysis for an entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetLineageImpactEndpoint : Fdw.Operations.Endpoints.GetLineageImpactEndpointBase
{
    /// <inheritdoc />
    public GetLineageImpactEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetLineageImpactEndpointBase> logger)
        : base(configurationGateway, pipelineProvider, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Lineage");
    }
}
