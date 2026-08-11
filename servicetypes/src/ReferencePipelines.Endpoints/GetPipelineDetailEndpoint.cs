using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;

namespace ReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get detailed pipeline configuration by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineDetailEndpoint : GetPipelineDetailEndpointBase
{
    /// <inheritdoc />
    public GetPipelineDetailEndpoint(PipelineServiceConfigurationProvider pipelineProvider)
        : base(pipelineProvider)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
