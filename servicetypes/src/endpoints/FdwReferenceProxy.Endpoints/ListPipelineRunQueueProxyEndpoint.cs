using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the pipeline run queue of the ETL server (FDW-3097).</summary>
/// <remarks>
/// The queue is the ETL server's in-memory scheduler state. The ETL server judges what the caller reaches (a platform administrator reads every
/// tenant, anyone else their own tenant), so nothing is filtered here.
/// </remarks>
public sealed class ListPipelineRunQueueProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListPipelineRunQueueProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public ListPipelineRunQueueProxyEndpoint(IHttpClientFactory factory, ILogger<ListPipelineRunQueueProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/queue", "pipelines:read", "etl/queue",
            "List the pipeline run queue (proxy)",
            "Forwards to the ETL server, which runs pipelines. Answers the runs in flight and the runs waiting, in the order they will start, each waiting run with the reason it cannot start now.",
            "EtlOrchestration"))
    {
    }
}
