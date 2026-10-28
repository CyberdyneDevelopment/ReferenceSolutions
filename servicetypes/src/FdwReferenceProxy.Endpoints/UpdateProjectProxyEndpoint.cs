using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the update of an ETL project to the ETL server.</summary>
public sealed class UpdateProjectProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="UpdateProjectProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public UpdateProjectProxyEndpoint(IHttpClientFactory factory, ILogger<UpdateProjectProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.PATCH, "/etl/projects/{id}", "projects:write", "projects/{id}",
            "Update an ETL project (proxy)",
            "Forwards to the ETL server, which patches fields on an existing project orchestration node.",
            "EtlProjects"))
    {
    }
}
