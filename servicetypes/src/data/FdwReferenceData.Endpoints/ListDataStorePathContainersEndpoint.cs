using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the containers of one path of a data store, one page at a time (FDW-1194).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStorePathContainersEndpoint : ListDataStorePathContainersEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataStorePathContainersEndpoint"/> class.</summary>
    public ListDataStorePathContainersEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<ListDataStorePathContainersEndpoint> logger)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
