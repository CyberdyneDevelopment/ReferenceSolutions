using System;
using Fdw.Services.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to update an existing DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateDataStoreEndpoint : UpdateDataStoreEndpointBase<DataStoreConfiguration>
{
    private readonly ILogger<UpdateDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDataStoreEndpoint"/> class.
    /// </summary>
    public UpdateDataStoreEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        ILogger<UpdateDataStoreEndpoint> logger)
        : base(dataStoreProvider, connectionProvider)
    {
        _logger = logger ?? NullLogger<UpdateDataStoreEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override DataStoreConfiguration UpdateConfiguration(
        DataStoreConfiguration existing,
        UpdateDataStoreRequest request,
        Guid? resolvedConnectionId)
    {
        if (request.Description != null)
            existing.Description = request.Description;

        if (request.WriteMode != null)
            existing.WriteMode = request.WriteMode;

        // Why: StoreType maps to ServiceOptionType internally. Only update if provided.
        if (request.StoreType != null)
            existing.ServiceOptionType = request.StoreType;

        if (request.DisplayName != null)
            existing.DisplayName = request.DisplayName;

        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;

        // Why: resolvedConnectionId is non-null only when request.ConnectionName was provided
        // and successfully resolved. Null means "keep existing connection".
        if (resolvedConnectionId.HasValue)
            existing.ConnectionId = resolvedConnectionId.Value;

        return existing;
    }

    /// <inheritdoc />
    protected override DataStoreDetailResponse MapToDetail(DataStoreConfiguration savedConfig, UpdateDataStoreRequest request)
    {
        if (string.IsNullOrEmpty(savedConfig.ServiceOptionType))
        {
            DataStoreLog.ServiceOptionTypeMissing(_logger, savedConfig.Name);
        }

        return new DataStoreDetailResponse
        {
            Id = savedConfig.Id,
            Name = savedConfig.Name,
            DisplayName = savedConfig.DisplayName,
            IsActive = savedConfig.IsActive,
            StoreType = savedConfig.ServiceOptionType,
            ConnectionId = savedConfig.ConnectionId,
            // Why: If the request changed the connection, return the new name; otherwise resolve from config
            ConnectionName = request.ConnectionName ?? string.Empty,
            Description = savedConfig.Description,
            WriteMode = savedConfig.WriteMode,
            Paths = savedConfig.Paths?.Select(p => new DataStorePathResponse
            {
                Id = p.Id,
                Name = p.Name,
                PathType = p.PathType ?? string.Empty,
                PathValue = p.PathValue,
                Description = p.Description
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
