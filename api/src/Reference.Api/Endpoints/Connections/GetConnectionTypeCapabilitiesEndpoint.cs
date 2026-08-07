using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Closure for the connection type capabilities endpoint.
/// Returns supported container types, field types, write modes, and path formats
/// for a given connection type.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetConnectionTypeCapabilitiesEndpoint : GetConnectionTypeCapabilitiesEndpointBase
{
    private readonly ILogger<GetConnectionTypeCapabilitiesEndpointBase> _logger;

    /// <inheritdoc />
    public GetConnectionTypeCapabilitiesEndpoint(
        ConnectionConfigurationProvider configProvider,
        ILogger<GetConnectionTypeCapabilitiesEndpointBase> logger)
        : base(configProvider, logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }

    /// <inheritdoc />
    protected override void OnBeforeGet(string identifier)
    {
        ConnectionLog.LoadingCapabilities(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        ConnectionLog.CapabilitiesTypeNotFound(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterGet(string identifier)
    {
        ConnectionLog.CapabilitiesLoaded(_logger, identifier);
    }
}
