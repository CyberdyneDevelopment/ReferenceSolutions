using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list all containers in a DataStore (flat list).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStoreContainersEndpoint : ListDataStoreContainersEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataStoreContainersEndpoint"/> class.
    /// </summary>
    public ListDataStoreContainersEndpoint(DataStoreConfigurationProvider dataStoreProvider)
        : base(dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataStoreContainerWithPathDto>>> LoadItems(CancellationToken ct)
    {
        // Why: No store name is available at list-all scope; log covers the operation boundary.
        DataStoreLog.ListingContainers(Logger, string.Empty);
        var result = await base.LoadItems(ct).ConfigureAwait(false);
        if (result.IsSuccess)
            DataStoreLog.ContainersListed(Logger, result.Value?.Count ?? 0, string.Empty);
        return result;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
