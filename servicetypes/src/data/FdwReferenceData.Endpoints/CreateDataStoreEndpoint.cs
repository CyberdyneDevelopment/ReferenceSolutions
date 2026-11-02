using System;
using Fdw.Services.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceDataStores.Endpoints.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to create a new DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateDataStoreEndpoint : CreateDataStoreEndpointBase<IDataStoreImplementationConfiguration>
{
    private readonly ILogger<CreateDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateDataStoreEndpoint"/> class.
    /// </summary>
    public CreateDataStoreEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        ISchemaInformationService schema,
        ILogger<CreateDataStoreEndpoint> logger)
        : base(logger, dataStoreProvider, connectionProvider, schema) {
        _logger = logger ?? NullLogger<CreateDataStoreEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> ValidateCreate(CreateDataStoreRequest request, CancellationToken ct)
    {
        // Why: the store's transport (Implementation) is inherited from its connection — there is no
        // client-supplied StoreType anymore. ConnectionName is the required input; the base resolves the
        // connection and copies its Implementation onto the config.
        if (string.IsNullOrWhiteSpace(request.ConnectionName))
        {
            return Task.FromResult<IGenericResult>(
                GenericResult.Failure(DataStoreLog.ConnectionNameRequired(_logger, request.Name)));
        }

        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <inheritdoc />
    protected override IDataStoreImplementationConfiguration CreateConfiguration(CreateDataStoreRequest request, Guid connectionId)
    {
        return new DataStoreImplementationConfiguration
        {
            // Why: API contract — IDs are uuid v7 for time-orderable persistence.
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            // Why: Implementation (transport) is set by the base from the resolved connection
            // (CreateDataStoreEndpointBase.Create copies connection.Implementation) — the store
            // inherits its transport from its connection; there is no client-supplied StoreType.
            ConnectionId = connectionId,
            Description = request.Description,
            WriteMode = request.WriteMode,
            IsActive = request.IsActive,
        };
    }

    /// <inheritdoc />
    protected override DataStoreDetailResponse MapToDetail(IDataStoreImplementationConfiguration savedConfig, CreateDataStoreRequest request)
    {
        return new DataStoreDetailResponse
        {
            Id = savedConfig.Id,
            Name = savedConfig.Name,
            IsActive = savedConfig.IsActive,
            StoreType = savedConfig.Implementation,
            ConnectionId = savedConfig.ConnectionId,
            ConnectionName = request.ConnectionName,
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
