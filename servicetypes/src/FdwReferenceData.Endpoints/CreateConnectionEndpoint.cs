using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// The create-connection endpoint, for any connection type — dispatch happens inside
/// <see cref="CreateConnectionEndpointBase"/> via <c>ConnectionTypes.ByName(request.ServiceType)</c>.
/// This class exists only to give FastEndpoints a route.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateConnectionEndpoint : CreateConnectionEndpointBase
{
    /// <inheritdoc />
    public CreateConnectionEndpoint(ConnectionConfigurationProvider connectionProvider, ILogger<CreateConnectionEndpoint> logger)
        : base(logger, connectionProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
