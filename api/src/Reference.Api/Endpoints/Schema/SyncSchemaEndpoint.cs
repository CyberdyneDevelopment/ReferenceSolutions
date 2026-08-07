using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
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
using Fdw.Operations.Endpoints;
using Fdw.Web.RestEndpoints.Logging;

namespace Reference.Api.Endpoints.Schema;

/// <summary>
/// Endpoint to sync database schema with stored DataStore configuration.
/// Route: POST /connections/{ConnectionName}/sync-schema
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SyncSchemaEndpoint : SyncSchemaEndpointBase
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ILogger<SyncSchemaEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncSchemaEndpoint"/> class.
    /// </summary>
    public SyncSchemaEndpoint(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider configProvider,
        DataStoreConfigurationProvider dataStoreProvider,
        ILogger<SyncSchemaEndpoint>? logger = null)
        : base(logger ?? NullLogger<SyncSchemaEndpoint>.Instance)
    {
        _connectionProvider = connectionProvider;
        _configProvider = configProvider;
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
        var connectionResult = await _connectionProvider.Get<IDataConnection>(request.ConnectionName, ct);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            return GenericResult<SyncSchemaResponse>.Failure(
                SchemaEndpointLog.SchemaConnectionNotFound(_logger, request.ConnectionName, "resolve"));
        }

        var allConfigsResult = await _configProvider.Get(ct).ConfigureAwait(false);
        var config = allConfigsResult.IsSuccess
            ? (allConfigsResult.Value ?? []).FirstOrDefault(c => string.Equals(c.Name, request.ConnectionName, StringComparison.OrdinalIgnoreCase))
            : null;

        var connectionType = config?.ConnectionType ?? config?.ServiceOptionType;
        if (string.IsNullOrEmpty(connectionType))
        {
            return GenericResult<SyncSchemaResponse>.Failure(
                EndpointLog.ValidationFailed(_logger, "Connection", request.ConnectionName, "Unable to determine connection type"));
        }

        var connType = ConnectionTypes.ByName(connectionType);
        if (connType == ConnectionTypes.NotFound || connType is not ISchemaDiscovery schemaDiscovery)
        {
            return GenericResult<SyncSchemaResponse>.Failure(
                SchemaEndpointLog.SchemaDiscoveryNotSupported(_logger, request.ConnectionName));
        }

        var discoveryResult = await schemaDiscovery.DiscoverSchema(
            connectionResult.Value, DataStoreDiscoveryOptions.Default, ct).ConfigureAwait(false);

        if (!discoveryResult.IsSuccess || discoveryResult.Value == null)
        {
            return discoveryResult.Messages.Count > 0
                ? GenericResult<SyncSchemaResponse>.Failure(discoveryResult.Messages.ToArray())
                : GenericResult<SyncSchemaResponse>.Failure(
                    SchemaEndpointLog.SchemaOperationFailed(_logger, new InvalidOperationException("Schema discovery returned no data"), "discover", request.ConnectionName));
        }

        var currentTables = discoveryResult.Value
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
                tables.Add($"{path.Path}.{container.Name}");
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
