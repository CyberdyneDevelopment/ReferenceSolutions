using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the resume of an execution to the ETL server.</summary>
public sealed class ResumeEtlExecutionProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ResumeEtlExecutionProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public ResumeEtlExecutionProxyEndpoint(IHttpClientFactory factory, ILogger<ResumeEtlExecutionProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.POST, "/etl/executions/{id}/resume", "pipelines:execute", "executions/{id}/resume",
            "Resume a paused execution (proxy)",
            "Forwards to the ETL server, which owns executions. Only an execution in the Paused state can be resumed.",
            "EtlOrchestration"))
    {
    }
}
