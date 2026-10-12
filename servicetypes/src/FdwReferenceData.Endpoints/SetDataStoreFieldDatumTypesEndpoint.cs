using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to set the datum type of one or more container fields of a DataStore (FDW-988).
/// </summary>
[ExcludeFromCodeCoverage]
public class SetDataStoreFieldDatumTypesEndpoint : SetDataStoreFieldDatumTypesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetDataStoreFieldDatumTypesEndpoint"/> class.
    /// </summary>
    public SetDataStoreFieldDatumTypesEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<SetDataStoreFieldDatumTypesEndpoint> logger)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Set data store field datum types";

    /// <inheritdoc />
    protected override string EndpointDescription => "Sets the datum type of one or more container fields of a data store in one save; an id the store lacks refuses the request.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
