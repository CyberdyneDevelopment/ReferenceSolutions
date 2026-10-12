using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to delete a pipeline.
/// Sealed closure of generic base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeletePipelineEndpoint : DeletePipelineEndpointBase<IPipelineImplementationConfiguration>
{
    /// <inheritdoc />
    public DeletePipelineEndpoint(PipelineServiceConfigurationProvider provider, ILogger<DeletePipelineEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
