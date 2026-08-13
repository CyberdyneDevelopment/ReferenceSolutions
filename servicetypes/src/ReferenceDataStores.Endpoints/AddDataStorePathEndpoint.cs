using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to add a named path to an existing DataStore.
/// POST /datastores/{Name}/paths
/// </summary>
[ExcludeFromCodeCoverage]
public class AddDataStorePathEndpoint : Endpoint<AddDataStorePathRequest, DataStorePathResponse>
{
    // Why: DataStoreConfigurationProvider carries GetWithChildren which loads the full path list,
    // enabling the duplicate-path-name guard without a separate query.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    // Why: Injected as the concrete DefaultConfigurationProvider<DataPathConfiguration,
    // DataPathConfigurationCommand> because RegisterDomainServices (in DataStoreConfigurationProvider)
    // registers that exact type — it is NOT registered as IServiceConfigurationProvider<DataPathConfiguration>.
    private readonly DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> _pathProvider;

    private readonly ILogger<AddDataStorePathEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddDataStorePathEndpoint"/> class.
    /// </summary>
    public AddDataStorePathEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> pathProvider,
        ILogger<AddDataStorePathEndpoint> logger)
    {
        _dataStoreProvider = dataStoreProvider;
        _pathProvider = pathProvider;
        _logger = logger ?? NullLogger<AddDataStorePathEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/datastores/{Name}/paths");
        Policies("datastores:write");
        Tags("DataStores");
        Summary(s =>
        {
            s.Summary = "Add a path to a DataStore";
            s.Description = "Creates a new DataPath entry within the named DataStore.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(AddDataStorePathRequest req, CancellationToken ct)
    {
        DataStoreLog.AddingPath(_logger, req.PathName, req.Name);

        // Why: Get now cascades the full Paths hierarchy (single source — no GetWithChildren verb),
        // so the duplicate-name guard below has the existing path list without a separate query.
        var storeResult = await _dataStoreProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
        {
            DataStoreLog.DataStoreNotFound(_logger, req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var store = storeResult.Value;

        if (store.Paths.Any(p => string.Equals(p.Name, req.PathName, StringComparison.Ordinal)))
        {
            DataStoreLog.PathAlreadyExists(_logger, req.PathName, req.Name);
            ThrowError($"A path named '{req.PathName}' already exists in DataStore '{req.Name}'", 409);
            return;
        }

        var path = new DataPathConfiguration
        {
            // Why: Mint a stable logical Id here so the response carries a durable identifier
            // the caller can use for subsequent container-add calls. DefaultConfigurationProvider.Save
            // uses this Id for INSERT (non-empty Id = upsert with this identity).
            Id = Guid.CreateVersion7(),
            Name = req.PathName,
            // Why: DataStoreId is the FK linking DataPath → DataStore (the parent's logical Id).
            DataStoreId = store.Id,
            // Why: Path is the per-feed addressing value the transport uses — a DB schema name, or
            // for an HTTP store the URL suffix appended to the base address (e.g. "all_hour.geojson").
            // Persist the caller's value verbatim; an absent Path is the empty string, matching
            // DataPathConfiguration.Path's own empty default (no value is invented).
            PathValue = req.Path,
            // Why: PathType is the DataPath discriminator ("Schema", "UrlSuffix", ...). Persist exactly
            // what the caller supplied so the type is never silently assumed.
            PathType = req.PathType,
            Description = req.Description,
        };

        var saveResult = await _pathProvider.Save(path, ct).ConfigureAwait(false);
        if (!saveResult.IsSuccess)
        {
            // Why: Log the failure via MessageLogging (log-and-return pattern). The provider's
            // own Save already logged the root cause; this entry records the endpoint-level context.
            DataStoreLog.AddPathFailed(_logger, req.PathName, req.Name, saveResult.CurrentMessage);
            HttpContext.Response.StatusCode = 500;
            return;
        }

        DataStoreLog.PathAddedSuccessfully(_logger, req.PathName, req.Name);

        await Send.ResponseAsync(new DataStorePathResponse
        {
            Id = path.Id,
            Name = path.Name,
            Description = path.Description,
            // Why: Echo the persisted addressing values back so the caller can confirm what was stored.
            // `?? string.Empty` matches the established DataStorePathResponse mapping (nullable config TypeId
            // → non-null DTO) used in GetDataStoreEndpointBase / CreateDataStoreEndpoint — DTO presentation
            // shape, not a configuration fallback; the persisted DataPathConfiguration.PathType stays null.
            PathType = path.PathType ?? string.Empty,
            PathValue = path.PathValue,
            Containers = [],
        }, 201, ct).ConfigureAwait(false);
    }
}
