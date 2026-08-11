using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceLineage.Endpoints;

/// <summary>
/// Concrete endpoint to get field-level lineage for a specific field within an entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetFieldLineageEndpoint : Fdw.Operations.Endpoints.GetFieldLineageEndpointBase
{
    /// <inheritdoc />
    public GetFieldLineageEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetFieldLineageEndpointBase> logger)
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
