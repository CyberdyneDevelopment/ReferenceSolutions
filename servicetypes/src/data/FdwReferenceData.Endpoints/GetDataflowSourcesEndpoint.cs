using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;
using Fdw.Services.Pipelines.Abstractions;

namespace FdwReferenceData.Endpoints;

/// <summary>
/// Concrete endpoint to get the sources and connections behind the dataflow graph.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataflowSourcesEndpoint : GetDataflowSourcesEndpointBase
{
    /// <inheritdoc />
    public GetDataflowSourcesEndpoint(
        Fdw.Operations.Endpoints.DataflowGraphConfigurationProvider provider,
        IPipelineConfigurationProvider pipelineProvider,
        ILogger<Fdw.Operations.Endpoints.GetDataflowSourcesEndpointBase> logger)
        : base(provider, pipelineProvider, logger)
    {
    }
}
