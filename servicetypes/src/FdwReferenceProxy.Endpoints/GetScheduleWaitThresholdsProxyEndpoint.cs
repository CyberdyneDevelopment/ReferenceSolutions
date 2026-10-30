using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

public sealed class GetScheduleWaitThresholdsProxyEndpoint : EtlRelayEndpointBase
{
    public GetScheduleWaitThresholdsProxyEndpoint(IHttpClientFactory factory, ILogger<GetScheduleWaitThresholdsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/wait-thresholds/schedules/{name}", "pipelines:read", "etl/wait-thresholds/schedules/{name}",
            "Read the thresholds on waiting runs a schedule states (proxy)",
            "Forwards to the ETL server, which holds the thresholds on waiting runs. Answers the mode (Inherit, Own or None) and the thresholds a schedule states.",
            "EtlOrchestration"))
    {
    }
}
