using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// The delete connection endpoint. Soft-deletes the whole connection aggregate — the typed body's children,
/// the typed body, then the conn.Connection header — and evicts the live instance from the provider cache.
/// </summary>
/// <remarks>
/// Why there is no per-type closure: the provider cascades the delete off the header's own discriminator, so
/// one endpoint serves every connection type. It used to be closed over MsSql purely to reach the typed
/// provider that deleted the body row.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DeleteConnectionEndpoint : DeleteConnectionEndpointBase
{
    /// <inheritdoc />
    public DeleteConnectionEndpoint(
        ConnectionConfigurationProvider connectionProvider,
        IConnectionProvider connectionLookupProvider,
        ILogger<DeleteConnectionEndpoint> logger)
        : base(connectionProvider, connectionLookupProvider, logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Connections");
    }
}
