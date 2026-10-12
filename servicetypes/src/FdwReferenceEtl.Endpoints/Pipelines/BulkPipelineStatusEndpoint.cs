using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get status of all pipelines in a single call.
/// </summary>
[ExcludeFromCodeCoverage]
public class BulkPipelineStatusEndpoint : BulkPipelineStatusEndpointBase
{
    /// <inheritdoc />
    public BulkPipelineStatusEndpoint(IConfigurationGatewayProvider configurationGateways, ILogger<BulkPipelineStatusEndpoint> logger) : base(logger, configurationGateways) {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
