using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Concrete endpoint to get dataset lineage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetLineageEndpoint : Fdw.Operations.Endpoints.GetDataSetLineageEndpoint
{
    /// <inheritdoc />
    public GetDataSetLineageEndpoint(
        IConfigurationGateway configurationGateway,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetDataSetLineageEndpoint> logger)
        : base(configurationGateway, pipelineProvider, logger)
    {
    }
}
