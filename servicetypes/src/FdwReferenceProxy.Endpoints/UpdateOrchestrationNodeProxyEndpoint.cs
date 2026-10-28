using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the update of an orchestration node to the ETL server.</summary>
public sealed class UpdateOrchestrationNodeProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="UpdateOrchestrationNodeProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public UpdateOrchestrationNodeProxyEndpoint(IHttpClientFactory factory, ILogger<UpdateOrchestrationNodeProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.PATCH, "/etl/nodes/{id}", "projects:write", "nodes/{id}",
            "Update an orchestration node (proxy)",
            "Forwards to the ETL server, which updates fields on an existing orchestration node.",
            "EtlOrchestrationNodes"))
    {
    }
}
