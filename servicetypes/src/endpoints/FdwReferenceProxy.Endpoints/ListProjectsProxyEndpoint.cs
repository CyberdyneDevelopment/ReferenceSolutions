using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the list of ETL projects to the ETL server.</summary>
public sealed class ListProjectsProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListProjectsProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public ListProjectsProxyEndpoint(IHttpClientFactory factory, ILogger<ListProjectsProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.GET, "/etl/projects", "projects:read", "projects",
            "List ETL projects (proxy)",
            "Forwards to the ETL server, which owns projects: every project-type orchestration node.",
            "EtlProjects"))
    {
    }
}
