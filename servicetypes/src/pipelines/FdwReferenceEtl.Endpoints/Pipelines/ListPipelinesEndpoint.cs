using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Tags;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to list all configured ETL pipelines.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPipelinesEndpoint : ListPipelinesEndpointBase
{
    /// <inheritdoc />
    public ListPipelinesEndpoint(
        PipelineServiceConfigurationProvider configProvider,
        ILogger<ListPipelinesEndpoint> logger,
        ITagService? tags = null)
        : base(logger, configProvider, tags) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
