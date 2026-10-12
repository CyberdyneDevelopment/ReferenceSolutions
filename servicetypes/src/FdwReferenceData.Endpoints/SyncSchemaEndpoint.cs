using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Endpoints;
using Fdw.Schema.Endpoints.Discovery;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Schema.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

namespace FdwReferenceSchema.Endpoints;

/// <summary>
/// Endpoint to sync database schema with stored DataStore configuration.
/// Route: POST /connections/{ConnectionName}/sync-schema
/// </summary>
[ExcludeFromCodeCoverage]
public class SyncSchemaEndpoint : SyncSchemaEndpointBase
{
    private readonly ISchemaInformationService _schema;
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ILogger<SyncSchemaEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSchemaEndpoint"/> class.
    /// </summary>
    public SyncSchemaEndpoint(
        ISchemaInformationService schema,
        DataStoreConfigurationProvider dataStoreProvider,
        ILogger<SyncSchemaEndpoint>? logger = null)
        : base(logger ?? NullLogger<SyncSchemaEndpoint>.Instance)
    {
        _schema = schema;
        _dataStoreProvider = dataStoreProvider;
        _logger = logger ?? NullLogger<SyncSchemaEndpoint>.Instance;
    }

    /// <inheritdoc/>
    protected override string PolicyName => "fdw:datastores:write";

    /// <inheritdoc/>
    protected override void OnBeforeConfiguring()
    {
        Tags("Schema");
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<SyncSchemaResponse>> SyncSchema(
        SyncSchemaRequest request,
        CancellationToken ct)
    {
        // The schema service runs the connection's chosen discovery option and says why when it cannot.
        var discoveryResult = await _schema.DiscoverContainers(
            request.ConnectionName, DataStoreDiscoveryOptions.Default, ct).ConfigureAwait(false);
        if (discoveryResult.IsFailure || discoveryResult.Value is not { } discovered)
            return discoveryResult.ToNewResult<SyncSchemaResponse>();

        var currentTables = discovered
            .Select(c => $"{c.Path.PathValue}.{c.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var storedTables = await GetStoredTables(request.ConnectionName, ct).ConfigureAwait(false);
        var storedSet = storedTables.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var response = new SyncSchemaResponse
        {
            DataStoreName = request.ConnectionName,
            AddedTables = currentTables.Except(storedSet, StringComparer.OrdinalIgnoreCase).ToList(),
            RemovedTables = storedSet.Except(currentTables, StringComparer.OrdinalIgnoreCase).ToList()
        };

        response.HasChanges = response.AddedTables.Count > 0 || response.RemovedTables.Count > 0;

        return GenericResult<SyncSchemaResponse>.Success(response);
    }

    private async Task<IReadOnlyList<string>> GetStoredTables(string connectionName, CancellationToken ct)
    {
        var allStoresResult = await _dataStoreProvider.Get(ct).ConfigureAwait(false);

        var dataStore = (allStoresResult.Value ?? [])
            .FirstOrDefault(ds => string.Equals(ds.Name, connectionName, StringComparison.OrdinalIgnoreCase));

        if (dataStore == null)
        {
            return [];
        }

        // Return container names from the DataStore's paths
        var tables = new List<string>();
        foreach (var path in dataStore.Paths)
        {
            foreach (var container in path.Containers)
            {
                tables.Add($"{path.PathValue}.{container.Name}");
            }
        }

        return tables;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Schema");
    }
}
