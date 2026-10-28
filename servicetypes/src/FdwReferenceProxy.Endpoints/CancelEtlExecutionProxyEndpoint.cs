using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the cancel of an execution to the ETL server.</summary>
public sealed class CancelEtlExecutionProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="CancelEtlExecutionProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public CancelEtlExecutionProxyEndpoint(IHttpClientFactory factory, ILogger<CancelEtlExecutionProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.POST, "/etl/executions/{id}/cancel", "pipelines:execute", "executions/{id}/cancel",
            "Cancel a running or paused execution (proxy)",
            "Forwards to the ETL server, which owns executions. A completed or failed execution cannot be cancelled.",
            "EtlOrchestration"))
    {
    }
}
