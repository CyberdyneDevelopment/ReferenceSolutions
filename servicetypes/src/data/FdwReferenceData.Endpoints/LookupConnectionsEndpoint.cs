using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Endpoint to look up connections by name, one page at a time, for pickers and name-taken checks (FDW-1194).
/// </summary>
[ExcludeFromCodeCoverage]
public class LookupConnectionsEndpoint : LookupConnectionsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupConnectionsEndpoint"/> class.</summary>
    public LookupConnectionsEndpoint(ConnectionConfigurationProvider configProvider, ILogger<LookupConnectionsEndpoint> logger)
        : base(logger, configProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
