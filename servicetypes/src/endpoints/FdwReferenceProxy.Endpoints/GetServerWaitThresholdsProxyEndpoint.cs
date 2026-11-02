using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

public sealed class GetServerWaitThresholdsProxyEndpoint : EtlRelayEndpointBase
{
    public GetServerWaitThresholdsProxyEndpoint(IHttpClientFactory factory, ILogger<GetServerWaitThresholdsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/wait-thresholds/server", "pipelines:read", "etl/wait-thresholds/server",
            "Read the thresholds on waiting runs the server states (proxy)",
            "Forwards to the ETL server, which holds the thresholds on waiting runs. Answers the mode (Inherit, Own or None) and the thresholds the server states.",
            "EtlOrchestration"))
    {
    }
}
