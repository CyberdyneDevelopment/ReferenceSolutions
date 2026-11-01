using System;
using Fdw.Services.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceDataStores.Endpoints.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to update an existing DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateDataStoreEndpoint : UpdateDataStoreEndpointBase<IDataStoreImplementationConfiguration>
{
    private readonly ILogger<UpdateDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDataStoreEndpoint"/> class.
    /// </summary>
    public UpdateDataStoreEndpoint(DataStoreConfigurationProvider dataStoreProvider, ConnectionConfigurationProvider connectionProvider, ILogger<UpdateDataStoreEndpoint> logger)
        : base(logger, dataStoreProvider, connectionProvider) {
        _logger = logger ?? NullLogger<UpdateDataStoreEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override IDataStoreImplementationConfiguration UpdateConfiguration(
        IDataStoreImplementationConfiguration existing,
        UpdateDataStoreRequest request,
        Guid? resolvedConnectionId)
    {
        if (request.Description != null)
            existing.Description = request.Description;

        if (request.WriteMode != null)
            existing.WriteMode = request.WriteMode;

        // Why: StoreType maps to Implementation internally. Only update if provided.
        if (request.StoreType != null)
            existing.Implementation = request.StoreType;

        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;

        // Why: resolvedConnectionId is non-null only when request.ConnectionName was provided
        // and successfully resolved. Null means "keep existing connection".
        if (resolvedConnectionId.HasValue)
            existing.ConnectionId = resolvedConnectionId.Value;

        return existing;
    }

    /// <inheritdoc />
    protected override DataStoreDetailResponse MapToDetail(IDataStoreImplementationConfiguration savedConfig, UpdateDataStoreRequest request)
    {
        if (string.IsNullOrEmpty(savedConfig.Implementation))
        {
            DataStoreLog.ImplementationMissing(_logger, savedConfig.Name);
        }

        return new DataStoreDetailResponse
        {
            Id = savedConfig.Id,
            Name = savedConfig.Name,
            IsActive = savedConfig.IsActive,
            StoreType = savedConfig.Implementation,
            ConnectionId = savedConfig.ConnectionId,
            // ConnectionName is set by UpdateDataStoreEndpointBase from the saved store's own ConnectionId
            // (a PATCH that leaves the connection alone carries no name to echo).
            Description = savedConfig.Description,
            ClassificationId = savedConfig.ClassificationId,
            WriteMode = savedConfig.WriteMode,
            Paths = savedConfig.Paths?.Select(p => new DataStorePathResponse
            {
                Id = p.Id,
                Name = p.Name,
                PathType = p.PathType ?? string.Empty,
                PathValue = p.PathValue,
                Description = p.Description,
                ClassificationId = p.ClassificationId
            }).ToList() ?? []
        };
    }

    /// <inheritdoc />
    protected override void OnBeforeUpdate(string identifier)
    {
        DataStoreLog.UpdatingDataStore(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        DataStoreLog.DataStoreNotFound(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterUpdate(string identifier)
    {
        DataStoreLog.DataStoreUpdated(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
