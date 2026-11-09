using Microsoft.Extensions.Logging;
using Fdw.Services.Connections.Abstractions;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Concrete endpoint to get connections filtered by type.
/// Overrides route to /connections/by-type/{Name} to avoid conflict with GET /connections/{Name}.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConnectionsByTypeEndpoint : Fdw.Services.Data.Endpoints.GetConnectionsByTypeEndpointBase
{
    /// <inheritdoc />
    public GetConnectionsByTypeEndpoint(IConnectionConfigurationProvider provider, ILogger<GetConnectionsByTypeEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override string Route => "/connections/by-type/{Name}";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
