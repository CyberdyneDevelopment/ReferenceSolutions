using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the run history of pipelines to the ETL server.</summary>
public sealed class ListPipelineHistoryProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListPipelineHistoryProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public ListPipelineHistoryProxyEndpoint(IHttpClientFactory factory, ILogger<ListPipelineHistoryProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/history", "pipelines:read", "etl/history",
            "List pipeline run history (proxy)",
            "Forwards to the ETL server, which owns pipeline execution history. A paginated list, filtered by pipeline name and success status through the same query as the ETL route.",
            "EtlOrchestration"))
    {
    }
}
