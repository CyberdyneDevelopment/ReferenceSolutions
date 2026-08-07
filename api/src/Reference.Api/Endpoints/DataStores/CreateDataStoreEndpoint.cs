using System;
using Fdw.Services.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to create a new DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataStoreEndpoint : CreateDataStoreEndpointBase<DataStoreConfiguration>
{
    private readonly ILogger<CreateDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateDataStoreEndpoint"/> class.
    /// </summary>
    public CreateDataStoreEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        ILogger<CreateDataStoreEndpoint> logger)
        : base(dataStoreProvider, connectionProvider)
    {
        _logger = logger ?? NullLogger<CreateDataStoreEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> ValidateCreate(CreateDataStoreRequest request, CancellationToken ct)
    {
        // Why: the store's transport (ServiceOptionType) is inherited from its connection — there is no
        // client-supplied StoreType anymore. ConnectionName is the required input; the base resolves the
        // connection and copies its ServiceOptionType onto the config.
        if (string.IsNullOrWhiteSpace(request.ConnectionName))
        {
            return Task.FromResult<IGenericResult>(
                GenericResult.Failure(DataStoreLog.ConnectionNameRequired(_logger, request.Name)));
        }

        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <inheritdoc />
    protected override DataStoreConfiguration CreateConfiguration(CreateDataStoreRequest request, Guid connectionId)
    {
        return new DataStoreConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            // Why: ServiceOptionType (transport) is set by the base from the resolved connection
            // (CreateDataStoreEndpointBase.Create copies connection.ServiceOptionType) — the store
            // inherits its transport from its connection; there is no client-supplied StoreType.
            ConnectionId = connectionId,
            Description = request.Description,
            WriteMode = request.WriteMode,
            DisplayName = request.DisplayName,
            IsActive = request.IsActive,
        };
    }

    /// <inheritdoc />
    protected override DataStoreDetailResponse MapToDetail(DataStoreConfiguration savedConfig, CreateDataStoreRequest request)
    {
        return new DataStoreDetailResponse
        {
            Id = savedConfig.Id,
            Name = savedConfig.Name,
            DisplayName = savedConfig.DisplayName,
            IsActive = savedConfig.IsActive,
            StoreType = savedConfig.ServiceOptionType,
            ConnectionId = savedConfig.ConnectionId,
            ConnectionName = request.ConnectionName,
            Description = savedConfig.Description,
            WriteMode = savedConfig.WriteMode,
            Paths = savedConfig.Paths?.Select(p => new DataStorePathResponse
            {
                Id = p.Id,
                Name = p.Name,
                PathType = p.PathType ?? string.Empty,
                Path = p.Path,
                Description = p.Description
            }).ToList() ?? []
        };
    }

    /// <inheritdoc />
    protected override void OnBeforeCreate(string resourceName)
    {
        DataStoreLog.CreatingDataStore(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        DataStoreLog.DataStoreAlreadyExists(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAfterCreate(string resourceName)
    {
        DataStoreLog.DataStoreCreated(_logger, resourceName);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
