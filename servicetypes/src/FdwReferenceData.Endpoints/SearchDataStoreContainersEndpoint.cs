using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to find the containers of a data store across all of its paths, one page at a time (FDW-1194).
/// </summary>
[ExcludeFromCodeCoverage]
public class SearchDataStoreContainersEndpoint : SearchDataStoreContainersEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="SearchDataStoreContainersEndpoint"/> class.</summary>
    public SearchDataStoreContainersEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<SearchDataStoreContainersEndpoint> logger)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
