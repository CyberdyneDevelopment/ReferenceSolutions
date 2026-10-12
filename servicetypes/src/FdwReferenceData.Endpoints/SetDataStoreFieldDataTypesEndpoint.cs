using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to set the portable data type of one or more container fields of a DataStore (FDW-1103).
/// </summary>
[ExcludeFromCodeCoverage]
public class SetDataStoreFieldDataTypesEndpoint : SetDataStoreFieldDataTypesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetDataStoreFieldDataTypesEndpoint"/> class.
    /// </summary>
    public SetDataStoreFieldDataTypesEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<SetDataStoreFieldDataTypesEndpoint> logger)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Set data store field data types";

    /// <inheritdoc />
    protected override string EndpointDescription => "Sets the portable data type of one or more container fields of a data store in one save; an id the store lacks, or a type that is not a DataTypes option, refuses the request.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
