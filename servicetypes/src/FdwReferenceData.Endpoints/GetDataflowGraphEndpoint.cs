using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;

namespace FdwReferenceData.Endpoints;

/// <summary>
/// Concrete endpoint to get the dataflow graph reachable from the configured pipelines.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataflowGraphEndpoint : GetDataflowGraphEndpointBase
{
    /// <inheritdoc />
    public GetDataflowGraphEndpoint(
        Fdw.Operations.Endpoints.DataflowGraphConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetDataflowGraphEndpointBase> logger)
        : base(provider, pipelineProvider, logger)
    {
    }
}
