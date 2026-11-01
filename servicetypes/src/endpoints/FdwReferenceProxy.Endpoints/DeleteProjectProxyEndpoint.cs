using System.Net.Http;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>Relays the delete of an ETL project to the ETL server.</summary>
public sealed class DeleteProjectProxyEndpoint : EtlRelayEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteProjectProxyEndpoint"/> class.</summary>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger.</param>
    public DeleteProjectProxyEndpoint(IHttpClientFactory factory, ILogger<DeleteProjectProxyEndpoint> logger)
        : base(factory, logger, new EtlRelay(
            Http.DELETE, "/etl/projects/{id}", "projects:write", "projects/{id}",
            "Delete an ETL project (proxy)",
            "Forwards to the ETL server, which soft-deletes the project orchestration node.",
            "EtlProjects"))
    {
    }
}
