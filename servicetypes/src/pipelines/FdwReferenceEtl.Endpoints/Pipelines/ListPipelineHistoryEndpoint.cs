using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to list all pipeline execution history with pagination.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPipelineHistoryEndpoint : ListPipelineHistoryEndpointBase
{
    /// <inheritdoc />
    public ListPipelineHistoryEndpoint(IDataGatewayProvider dataGateways, ILogger<ListPipelineHistoryEndpoint> logger) : base(logger, dataGateways) {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
