using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to read a graph pipeline as the one shape a graph is read in.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetGraphEndpoint : GetGraphEndpointBase
{
    /// <inheritdoc />
    public GetGraphEndpoint(IPipelineConfigurationProvider provider, ILogger<GetGraphEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
