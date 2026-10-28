using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the delete of an orchestration node to the ETL server.</summary>
public sealed class DeleteOrchestrationNodeProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteOrchestrationNodeProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public DeleteOrchestrationNodeProxyEndpoint(IHttpClientFactory factory, ILogger<DeleteOrchestrationNodeProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.DELETE, "/etl/nodes/{id}", "projects:write", "nodes/{id}",
            "Delete an orchestration node (proxy)",
            "Forwards to the ETL server, which soft-deletes the orchestration node.",
            "EtlOrchestrationNodes"))
    {
    }
}
