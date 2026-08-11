using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to get container details with fields.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetContainerEndpoint : GetContainerEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetContainerEndpoint"/> class.
    /// </summary>
    public GetContainerEndpoint(DataStoreConfigurationProvider dataStoreProvider)
        : base(dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override void OnBeforeGet(string identifier)
    {
        // Why: identifier is "{storeName}/{pathName}/{containerName}" at this hook.
        DataStoreLog.FetchingContainer(Logger, identifier, string.Empty, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        DataStoreLog.ContainerNotFound(Logger, identifier, string.Empty, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnAfterGet(string identifier)
    {
        DataStoreLog.ContainerRetrieved(Logger, identifier, 0);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
