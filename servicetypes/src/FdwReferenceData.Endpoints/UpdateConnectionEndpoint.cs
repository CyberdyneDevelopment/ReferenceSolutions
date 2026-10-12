using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// The update-connection endpoint, for any connection type — dispatch happens inside
/// <see cref="UpdateConnectionEndpointBase"/> via the existing record's own
/// <c>Implementation</c>. This class exists only to give FastEndpoints a route.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateConnectionEndpoint : UpdateConnectionEndpointBase
{
    /// <inheritdoc />
    public UpdateConnectionEndpoint(ConnectionConfigurationProvider connectionProvider, ILogger<UpdateConnectionEndpoint> logger)
        : base(logger, connectionProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
