using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to get one container's fields, read without the rest of its data store (FDW-951).
/// </summary>
[ExcludeFromCodeCoverage]
public class GetDataStoreContainerFieldsEndpoint : GetDataStoreContainerFieldsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataStoreContainerFieldsEndpoint"/> class.
    /// </summary>
    public GetDataStoreContainerFieldsEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<GetDataStoreContainerFieldsEndpoint> logger)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Get container fields";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns the fields of one container of a data store, in ordinal order.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
