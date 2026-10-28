using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays one ETL project to the ETL server.</summary>
public sealed class GetProjectProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetProjectProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public GetProjectProxyEndpoint(IHttpClientFactory factory, ILogger<GetProjectProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/projects/{id}", "projects:read", "projects/{id}",
            "Get an ETL project by ID (proxy)",
            "Forwards to the ETL server, which returns the full project tree (stages, steps, pipelines).",
            "EtlProjects"))
    {
    }
}
