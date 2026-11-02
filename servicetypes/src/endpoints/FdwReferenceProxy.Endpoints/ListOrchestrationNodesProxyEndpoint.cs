using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the list of root orchestration nodes to the ETL server.</summary>
public sealed class ListOrchestrationNodesProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListOrchestrationNodesProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public ListOrchestrationNodesProxyEndpoint(IHttpClientFactory factory, ILogger<ListOrchestrationNodesProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/nodes", "projects:read", "nodes",
            "List root orchestration nodes (proxy)",
            "Forwards to the ETL server, which returns every root-level orchestration node (no parent).",
            "EtlOrchestrationNodes"))
    {
    }
}
