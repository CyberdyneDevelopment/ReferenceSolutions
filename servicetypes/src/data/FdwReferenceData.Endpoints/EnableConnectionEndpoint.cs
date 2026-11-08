using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Concrete endpoint that enables a disabled connection again and records why (FDW-3951). A threshold disables a connection and only a person enables it.
/// </summary>
[ExcludeFromCodeCoverage]
public class EnableConnectionEndpoint : EnableConnectionEndpointBase
{
    /// <inheritdoc />
    public EnableConnectionEndpoint(
        ConnectionConfigurationProvider connections,
        IConnectionDisabledStates states,
        ILogger<EnableConnectionEndpointBase> logger)
        : base(connections, states, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint() => Tags("Connections");
}
