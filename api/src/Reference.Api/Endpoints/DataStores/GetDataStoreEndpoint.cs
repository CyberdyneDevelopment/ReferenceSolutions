using System.Collections.Generic;
using Fdw.Services.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get a DataStore by name.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataStoreEndpoint : GetDataStoreEndpointBase
{
    private readonly ILogger<GetDataStoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataStoreEndpoint"/> class.
    /// </summary>
    public GetDataStoreEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        // Why: connections monitor required to resolve ConnectionId → ConnectionName in the detail DTO.
        ConnectionConfigurationProvider connectionProvider,
        ILogger<GetDataStoreEndpoint> logger)
        : base(dataStoreProvider, connectionProvider)
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
