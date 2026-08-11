using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using ReferenceDataStores.Endpoints.Logging;
using Fdw.Services.Data.Clients.Models;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to trigger schema discovery for a DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public class DiscoverDataStoreEndpoint : DiscoverDataStoreEndpointBase
{
    private readonly IDataStoreProvider _dataStoreProvider;
    private readonly ILogger<DiscoverDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverDataStoreEndpoint"/> class.
    /// </summary>
    public DiscoverDataStoreEndpoint(
        IDataStoreProvider dataStoreProvider,
        ILoggerFactory loggerFactory)
        : base(dataStoreProvider, loggerFactory)
    {
        _dataStoreProvider = dataStoreProvider;
        // Why: DiscoverDataStoreEndpointBase doesn't expose a Logger property; resolve from factory directly.
        _logger = loggerFactory.CreateLogger<DiscoverDataStoreEndpoint>();
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DiscoveryResultPayload>> PerformDiscovery(
        string dataStoreName,
        bool refresh,
        CancellationToken ct)
    {
        DataStoreLog.StartingDiscovery(_logger, dataStoreName);

        // Verify the data store exists
        var dataStoreResult = await _dataStoreProvider.Get(dataStoreName, ct).ConfigureAwait(false);
        if (dataStoreResult.IsFailure)
        {
            DataStoreLog.DiscoveryFailed(_logger, dataStoreName, string.Join("; ", dataStoreResult.Messages));
            return GenericResult<DiscoveryResultPayload>.Failure(dataStoreResult.Messages.ToArray());
        }

        var dataStore = dataStoreResult.Value!;

        // Why: the provider-resolved store is the canonical IDataNode tree exposing Paths; report the
        // discovered path count here (container-level detail is on-demand via dot-walk).
        var containerCount = dataStore.Paths.Count;

        DataStoreLog.DiscoveryCompleted(_logger, dataStoreName, containerCount, 0);

        return GenericResult<DiscoveryResultPayload>.Success(new DiscoveryResultPayload
        {
            DataStoreName = dataStore.Name,
            ContainerCount = containerCount,
            WasRefreshed = refresh
        });
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("DataStores");
    }
}
