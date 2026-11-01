using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the creation of an ETL project to the ETL server.</summary>
public sealed class CreateProjectProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="CreateProjectProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public CreateProjectProxyEndpoint(IHttpClientFactory factory, ILogger<CreateProjectProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.POST, "/etl/projects", "projects:write", "projects",
            "Create an ETL project (proxy)",
            "Forwards to the ETL server, which creates a root-level project orchestration node.",
            "EtlProjects"))
    {
    }
}
