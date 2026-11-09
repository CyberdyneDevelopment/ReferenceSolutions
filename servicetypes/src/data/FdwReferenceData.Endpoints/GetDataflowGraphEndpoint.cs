using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;
using Fdw.Services.Pipelines.Abstractions;

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
        IPipelineConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetDataflowGraphEndpointBase> logger)
        : base(provider, pipelineProvider, logger)
    {
    }
}
