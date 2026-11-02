using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

public sealed class PutScheduleWaitThresholdsProxyEndpoint : EtlRelayEndpointBase
{
    public PutScheduleWaitThresholdsProxyEndpoint(IHttpClientFactory factory, ILogger<PutScheduleWaitThresholdsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.PUT, "/etl/wait-thresholds/schedules/{name}", "pipelines:write", "etl/wait-thresholds/schedules/{name}",
            "State the thresholds on waiting runs a schedule states (proxy)",
            "Forwards to the ETL server, which holds the thresholds on waiting runs. Answers the mode (Inherit, Own or None) and the thresholds a schedule states.",
            "EtlOrchestration"))
    {
    }
}
