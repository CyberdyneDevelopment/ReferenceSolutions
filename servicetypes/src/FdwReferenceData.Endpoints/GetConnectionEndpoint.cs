using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Type-agnostic get-connection endpoint. The base maps any connection type through that type's own
/// endpoint behavior, resolved off the <c>ConnectionTypes</c> registry rather than a switch on the type
/// name, so this class only gives the route its tag.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConnectionEndpoint : GetConnectionEndpointBase
{
    /// <inheritdoc />
    public GetConnectionEndpoint(ConnectionConfigurationProvider configProvider, ILogger<GetConnectionEndpoint> logger)
        : base(logger, configProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
