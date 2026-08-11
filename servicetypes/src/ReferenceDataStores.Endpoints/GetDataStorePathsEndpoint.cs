using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to get all paths for a DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataStorePathsEndpoint : GetDataStorePathsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataStorePathsEndpoint"/> class.
    /// </summary>
    public GetDataStorePathsEndpoint(DataStoreConfigurationProvider dataStoreProvider)
        : base(dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/paths";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get data store paths";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all paths for a specific data store.";

    /// <inheritdoc />
    protected override void OnBeforeGet(string identifier)
    {
        DataStoreLog.FetchingPaths(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        DataStoreLog.DataStoreNotFound(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterGet(string identifier)
    {
        DataStoreLog.PathsRetrieved(Logger, identifier, 0);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
