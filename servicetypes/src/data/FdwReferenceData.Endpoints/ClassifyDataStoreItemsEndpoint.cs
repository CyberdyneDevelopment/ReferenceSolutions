using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to set the data classification of one or more paths, containers or fields of a DataStore.
/// </summary>
[ExcludeFromCodeCoverage]
public class ClassifyDataStoreItemsEndpoint : ClassifyDataStoreItemsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassifyDataStoreItemsEndpoint"/> class.
    /// </summary>
    public ClassifyDataStoreItemsEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<ClassifyDataStoreItemsEndpoint> logger)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Classify data store items";

    /// <inheritdoc />
    protected override string EndpointDescription => "Sets the data classification of one or more paths, containers or fields of a data store in one save; an id the store lacks refuses the request.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
