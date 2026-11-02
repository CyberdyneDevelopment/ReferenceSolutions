using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the creation of an orchestration node to the ETL server.</summary>
public sealed class CreateOrchestrationNodeProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="CreateOrchestrationNodeProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public CreateOrchestrationNodeProxyEndpoint(IHttpClientFactory factory, ILogger<CreateOrchestrationNodeProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.POST, "/etl/nodes", "projects:write", "nodes",
            "Create an orchestration node (proxy)",
            "Forwards to the ETL server, which creates an orchestration node of any type.",
            "EtlOrchestrationNodes"))
    {
    }
}
