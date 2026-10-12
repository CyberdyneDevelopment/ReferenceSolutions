using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get detailed pipeline configuration by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineDetailEndpoint : GetPipelineDetailEndpointBase
{
    /// <inheritdoc />
    public GetPipelineDetailEndpoint(
        PipelineServiceConfigurationProvider pipelineProvider,
        ITransformationConfigurationProvider transformations,
        ILogger<GetPipelineDetailEndpoint> logger)
        : base(logger, pipelineProvider, transformations) {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
