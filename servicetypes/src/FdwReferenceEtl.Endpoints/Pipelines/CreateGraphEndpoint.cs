using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to create a graph pipeline.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateGraphEndpoint : CreateGraphEndpointBase
{
    /// <inheritdoc />
    public CreateGraphEndpoint(
        PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations, ILogger<CreateGraphEndpoint> logger)
        : base(logger, provider, transformations)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
