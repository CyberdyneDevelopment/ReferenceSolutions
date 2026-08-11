using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceLineage.Endpoints;

/// <summary>
/// Concrete endpoint to get the transitive lineage graph for an entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetLineageGraphEndpoint : Fdw.Operations.Endpoints.GetLineageGraphEndpointBase
{
    /// <inheritdoc />
    public GetLineageGraphEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetLineageGraphEndpointBase> logger)
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
