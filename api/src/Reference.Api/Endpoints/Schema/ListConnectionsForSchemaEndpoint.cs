using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Schema.Endpoints.Discovery;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Api.Endpoints.Schema;

/// <summary>
/// Endpoint to get available connections for schema discovery.
/// Route: GET /connections/schema-capable
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListConnectionsForSchemaEndpoint : ListSchemaCapableConnectionsEndpointBase
{
    // Why: ConnectionConfigurationProvider replaces IOptionsMonitor<List<T>> — provides dual-source
    // (ctrl + cfg) connection resolution through DefaultConfigurationProvider pattern.
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
        var schemaCapableTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MsSql",
            "PostgreSql",
            "MySql",
            "Oracle"
        };

        // Why: Provider.GetAll replaces _configurations.CurrentValue — dual-source (ctrl + cfg).
        var allConnectionsResult = await _provider.Get(ct).ConfigureAwait(false);
        var connections = (allConnectionsResult.Value ?? [])
            .Where(c => schemaCapableTypes.Contains(c.ServiceOptionType ?? string.Empty))
            .Select(c => new ConnectionInfoDto
            {
                Name = c.Name,
                Type = c.ServiceOptionType ?? "Unknown",
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
