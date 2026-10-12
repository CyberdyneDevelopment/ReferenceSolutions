using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Closure for the list connections endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionsEndpoint : ListConnectionsEndpointBase
{
    /// <inheritdoc />
    public ListConnectionsEndpoint(ConnectionConfigurationProvider configProvider, ILogger<ListConnectionsEndpoint> logger)
        : base(logger, configProvider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
