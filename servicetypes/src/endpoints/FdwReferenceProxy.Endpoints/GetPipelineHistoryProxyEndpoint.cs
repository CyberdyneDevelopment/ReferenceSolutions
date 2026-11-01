using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the run history of one pipeline to the ETL server.</summary>
public sealed class GetPipelineHistoryProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetPipelineHistoryProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public GetPipelineHistoryProxyEndpoint(IHttpClientFactory factory, ILogger<GetPipelineHistoryProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/pipelines/{Name}/history", "pipelines:read", "etl/pipelines/{Name}/history",
            "Get the run history of one pipeline (proxy)",
            "Forwards to the ETL server, which returns the execution history of the named pipeline.",
            "EtlOrchestration"))
    {
    }
}
