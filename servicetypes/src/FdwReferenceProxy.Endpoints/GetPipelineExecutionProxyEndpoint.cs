using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays one run from the run history to the ETL server.</summary>
public sealed class GetPipelineExecutionProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetPipelineExecutionProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public GetPipelineExecutionProxyEndpoint(IHttpClientFactory factory, ILogger<GetPipelineExecutionProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/history/{ExecutionId}", "pipelines:read", "etl/history/{ExecutionId}",
            "Get the details of a pipeline run (proxy)",
            "Forwards to the ETL server, which returns the details of one pipeline execution by its execution ID.",
            "EtlOrchestration"))
    {
    }
}
