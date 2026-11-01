using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceDataStores.Endpoints.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>Endpoint to delete a DataStore.</summary>
[ExcludeFromCodeCoverage]
public class DeleteDataStoreEndpoint : DeleteDataStoreEndpointBase<IDataStoreImplementationConfiguration>
{
    private readonly ILogger<DeleteDataStoreEndpoint> _logger;

    /// <inheritdoc />
    public DeleteDataStoreEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<DeleteDataStoreEndpoint> logger)
        : base(logger, dataStoreProvider) {
        _logger = logger ?? NullLogger<DeleteDataStoreEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void OnBeforeDelete(string identifier)
    {
        DataStoreLog.DeletingDataStore(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        DataStoreLog.DataStoreNotFound(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterDelete(string identifier)
    {
        DataStoreLog.DataStoreDeleted(_logger, identifier);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
