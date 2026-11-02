using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get pipeline execution history for a specific pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineHistoryEndpoint : GetPipelineHistoryEndpointBase
{
    /// <inheritdoc />
    public GetPipelineHistoryEndpoint(IDataGatewayProvider dataGateways, ILogger<GetPipelineHistoryEndpoint> logger) : base(logger, dataGateways) {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
