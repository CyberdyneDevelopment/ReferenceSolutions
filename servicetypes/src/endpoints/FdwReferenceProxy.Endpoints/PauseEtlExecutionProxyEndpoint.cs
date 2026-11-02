using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the pause of an execution to the ETL server.</summary>
public sealed class PauseEtlExecutionProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="PauseEtlExecutionProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public PauseEtlExecutionProxyEndpoint(IHttpClientFactory factory, ILogger<PauseEtlExecutionProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.POST, "/etl/executions/{id}/pause", "pipelines:execute", "executions/{id}/pause",
            "Pause a running execution (proxy)",
            "Forwards to the ETL server, which owns executions. Only an execution in the Running state can be paused.",
            "EtlOrchestration"))
    {
    }
}
