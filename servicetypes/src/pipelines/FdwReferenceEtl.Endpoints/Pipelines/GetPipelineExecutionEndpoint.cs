using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get a specific pipeline execution record.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineExecutionEndpoint : GetPipelineExecutionEndpointBase
{
    /// <inheritdoc />
    public GetPipelineExecutionEndpoint(IDataGatewayProvider dataGateways, ILogger<GetPipelineExecutionEndpoint> logger) : base(logger, dataGateways) {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
