using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// Closure for the get-connection-health-history endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConnectionHealthEndpoint : GetConnectionHealthEndpointBase
{
    /// <inheritdoc />
    public GetConnectionHealthEndpoint(
        ConnectionConfigurationProvider configProvider,
        IConnectionHealthService healthService,
        ILogger<GetConnectionHealthEndpoint> logger)
        : base(configProvider, healthService, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Connections");
    }
}
