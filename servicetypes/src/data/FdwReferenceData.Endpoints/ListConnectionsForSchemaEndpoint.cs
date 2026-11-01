using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Schema.Endpoints.Discovery;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchema.Endpoints;

/// <summary>
/// Endpoint to get available connections for schema discovery.
/// Route: GET /connections/schema-capable
/// </summary>
[ExcludeFromCodeCoverage]
public class ListConnectionsForSchemaEndpoint : ListSchemaCapableConnectionsEndpointBase
{
    // Why: ConnectionConfigurationProvider replaces IOptionsMonitor<List<T>> — provides dual-source
    // (ctrl + cfg) connection resolution through ImplementationConfigurationProviderBase pattern.
    private readonly ConnectionConfigurationProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListConnectionsForSchemaEndpoint"/> class.
    /// </summary>
    public ListConnectionsForSchemaEndpoint(
        ConnectionConfigurationProvider provider,
        ILogger<ListConnectionsForSchemaEndpoint>? logger = null)
        : base(logger ?? NullLogger<ListConnectionsForSchemaEndpoint>.Instance)
    {
        _provider = provider;
    }

    // Why: bare "{resource}:{action}" policy names — FdwAuthorizationPolicyProvider splits on the
    // first ':' so a stale "fdw:" prefix resolves to resource="fdw"/action="datastores:read", which
    // matches no seeded permission and 403s every request. The "fdw:" brand prefix was removed
    // framework-wide; the seeded permission is "datastores:read".
    /// <inheritdoc/>
    protected override string PolicyName => "datastores:read";

    /// <inheritdoc/>
    protected override void OnBeforeConfiguring()
    {
        Tags("Schema");
    }

    /// <inheritdoc/>
#pragma warning disable MA0016 // Return type constrained by endpoint generic parameter
    protected override async Task<List<ConnectionInfoDto>> GetSchemaCapableConnections(CancellationToken ct)
#pragma warning restore MA0016
    {
        // Schema-capable is the connection's own choice: its SchemaDiscovery option discovers. Read off
        // the option rather than a list of connection type names, which listed MsSql connections that
        // chose None and would never list a kind that gains a discovery option.
        var allConnectionsResult = await _provider.Get(ct).ConfigureAwait(false);
        var connections = (allConnectionsResult.Value ?? [])
            .Where(c => SchemaDiscoveryTypes.ByName(c.SchemaDiscovery).DiscoversSchema)
            .Select(c => new ConnectionInfoDto
            {
                Name = c.Name,
                Type = c.Implementation,
                SupportsSchemaDiscovery = true
            })
            .ToList();

        return connections;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Schema");
    }
}
