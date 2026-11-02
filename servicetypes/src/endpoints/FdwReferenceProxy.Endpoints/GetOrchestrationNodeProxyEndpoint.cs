using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays one orchestration node to the ETL server.</summary>
public sealed class GetOrchestrationNodeProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetOrchestrationNodeProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public GetOrchestrationNodeProxyEndpoint(IHttpClientFactory factory, ILogger<GetOrchestrationNodeProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/nodes/{id}", "projects:read", "nodes/{id}",
            "Get an orchestration node (proxy)",
            "Forwards to the ETL server, which returns a single orchestration node by ID, with no children loaded.",
            "EtlOrchestrationNodes"))
    {
    }
}
