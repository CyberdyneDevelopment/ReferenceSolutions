using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to get container details by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetContainerByIdEndpoint : GetContainerByIdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetContainerByIdEndpoint"/> class.
    /// </summary>
    public GetContainerByIdEndpoint(DataStoreConfigurationProvider dataStoreProvider)
        : base(dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/by-id/{{Id}}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get container by ID";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns container details by its unique identifier.";

    /// <inheritdoc />
    protected override void OnBeforeGet(string identifier)
    {
        // Why: identifier here is the Guid string of the container ID.
        if (System.Guid.TryParse(identifier, out var id))
            DataStoreLog.FetchingContainerById(Logger, id);
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
