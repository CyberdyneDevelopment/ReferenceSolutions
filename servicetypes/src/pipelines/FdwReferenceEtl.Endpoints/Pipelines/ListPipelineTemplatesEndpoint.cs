using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to list the pipelines that are templates.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPipelineTemplatesEndpoint : ListPipelineTemplatesEndpointBase
{
    /// <inheritdoc />
    public ListPipelineTemplatesEndpoint(
        PipelineServiceConfigurationProvider configProvider,
        ILogger<ListPipelineTemplatesEndpoint> logger)
        : base(logger, configProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
