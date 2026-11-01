using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

public sealed class GetPipelineWaitThresholdsProxyEndpoint : EtlRelayEndpointBase
{
    public GetPipelineWaitThresholdsProxyEndpoint(IHttpClientFactory factory, ILogger<GetPipelineWaitThresholdsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/wait-thresholds/pipelines/{name}", "pipelines:read", "etl/wait-thresholds/pipelines/{name}",
            "Read the thresholds on waiting runs a pipeline states (proxy)",
            "Forwards to the ETL server, which holds the thresholds on waiting runs. Answers the mode (Inherit, Own or None) and the thresholds a pipeline states.",
            "EtlOrchestration"))
    {
    }
}
