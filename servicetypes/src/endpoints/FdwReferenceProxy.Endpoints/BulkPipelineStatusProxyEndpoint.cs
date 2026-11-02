using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the status of every pipeline to the ETL server.</summary>
public sealed class BulkPipelineStatusProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="BulkPipelineStatusProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public BulkPipelineStatusProxyEndpoint(IHttpClientFactory factory, ILogger<BulkPipelineStatusProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/pipelines/status", "pipelines:read", "etl/pipelines/status",
            "Get the status of every pipeline (proxy)",
            "Forwards to the ETL server, which returns the current status of all configured pipelines in one call.",
            "EtlOrchestration"))
    {
    }
}
