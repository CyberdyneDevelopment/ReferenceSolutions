using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

public sealed class PutServerWaitThresholdsProxyEndpoint : EtlRelayEndpointBase
{
    public PutServerWaitThresholdsProxyEndpoint(IHttpClientFactory factory, ILogger<PutServerWaitThresholdsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.PUT, "/etl/wait-thresholds/server", "pipelines:write", "etl/wait-thresholds/server",
            "State the thresholds on waiting runs the server states (proxy)",
            "Forwards to the ETL server, which holds the thresholds on waiting runs. Answers the mode (Inherit, Own or None) and the thresholds the server states.",
            "EtlOrchestration"))
    {
    }
}
