using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Closure for the list connections endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListConnectionsEndpoint : ListConnectionsEndpointBase
{
    /// <inheritdoc />
    public ListConnectionsEndpoint(ConnectionConfigurationProvider configProvider)
        : base(configProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
