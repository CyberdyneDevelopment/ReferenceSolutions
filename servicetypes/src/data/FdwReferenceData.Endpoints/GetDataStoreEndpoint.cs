using Fdw.Services.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceDataStores.Endpoints.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to get a DataStore by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataStoreEndpoint : GetDataStoreEndpointBase
{
    private readonly ILogger<GetDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataStoreEndpoint"/> class.
    /// </summary>
    public GetDataStoreEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        // Why: connections monitor required to resolve ConnectionId → ConnectionName in the detail DTO.
        ConnectionConfigurationProvider connectionProvider,
        IAuthorizationService authorization,
        ILogger<GetDataStoreEndpoint> logger)
        : base(logger, dataStoreProvider, connectionProvider, authorization)
    {
        _logger = logger ?? NullLogger<GetDataStoreEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void OnBeforeGet(string identifier)
    {
        DataStoreLog.FetchingDataStore(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        DataStoreLog.DataStoreNotFound(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterGet(string identifier)
    {
        DataStoreLog.DataStoreRetrieved(_logger, identifier, 0);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
