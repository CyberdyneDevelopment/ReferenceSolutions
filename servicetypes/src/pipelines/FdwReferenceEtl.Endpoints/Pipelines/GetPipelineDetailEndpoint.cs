using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Abstractions;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get detailed pipeline configuration by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineDetailEndpoint : GetPipelineDetailEndpointBase
{
    /// <inheritdoc />
    public GetPipelineDetailEndpoint(
        IPipelineConfigurationProvider pipelineProvider,
        ILogger<GetPipelineDetailEndpoint> logger)
        : base(logger, pipelineProvider) {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
