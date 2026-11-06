using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

public sealed class PutPipelineWaitThresholdsProxyEndpoint : EtlRelayEndpointBase
{
    public PutPipelineWaitThresholdsProxyEndpoint(IHttpClientFactory factory, ILogger<PutPipelineWaitThresholdsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.PATCH, "/etl/wait-thresholds/pipelines/{name}", "pipelines:write", "etl/wait-thresholds/pipelines/{name}",
            "State the thresholds on waiting runs a pipeline states (proxy)",
            "Forwards to the ETL server, which holds the thresholds on waiting runs. Answers the mode (Inherit, Own or None) and the thresholds a pipeline states.",
            "EtlOrchestration"))
    {
    }
}
