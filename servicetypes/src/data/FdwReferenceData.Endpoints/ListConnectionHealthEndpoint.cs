using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Closure for the list-current-connection-health endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionHealthEndpoint : ListConnectionHealthEndpointBase
{
    /// <inheritdoc />
    public ListConnectionHealthEndpoint(
        ILogger<ListConnectionHealthEndpoint> logger,
        IConnectionHealthService healthService)
        : base(logger, healthService)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Connections");
    }
}
