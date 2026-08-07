using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get the dataflow graph for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataflowGraphEndpoint : Fdw.Operations.Endpoints.GetDataflowGraphEndpoint
{
    /// <inheritdoc />
    public GetDataflowGraphEndpoint(
        Fdw.Operations.Endpoints.DataflowGraphConfigurationProvider provider,
        ILogger<Fdw.Operations.Endpoints.GetDataflowGraphEndpoint> logger)
        : base(provider, logger)
    {
    }
}

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

/// <summary>
/// Concrete endpoint to get impact analysis for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetImpactAnalysisEndpoint : Fdw.Operations.Endpoints.GetImpactAnalysisEndpoint
{
    /// <inheritdoc />
    public GetImpactAnalysisEndpoint(
        IConfigurationGateway configurationGateway,
        ILogger<Fdw.Operations.Endpoints.GetImpactAnalysisEndpoint> logger)
        : base(configurationGateway, logger)
    {
    }
}
