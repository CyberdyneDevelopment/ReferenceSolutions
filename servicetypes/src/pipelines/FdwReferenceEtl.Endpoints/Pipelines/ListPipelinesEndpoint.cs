using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;

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
        ILogger<ListPipelinesEndpoint> logger)
        : base(logger, configProvider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
