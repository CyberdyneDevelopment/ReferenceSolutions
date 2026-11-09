using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Tags;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Closure for the list connections endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionsEndpoint : ListConnectionsEndpointBase
{
    /// <inheritdoc />
    public ListConnectionsEndpoint(IConnectionConfigurationProvider configProvider, IConnectionDisabledStates states, ILogger<ListConnectionsEndpoint> logger, ITagService? tags = null)
        : base(logger, configProvider, states, tags) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
