using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using ReferenceDataStores.Endpoints.Logging;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list all DataStores.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStoresEndpoint : ListDataStoresEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataStoresEndpoint"/> class.
    /// </summary>
    public ListDataStoresEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider)
        : base(dataStoreProvider, connectionProvider)
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataStoreSummaryResponse>>> LoadItems(CancellationToken ct)
    {
        DataStoreLog.ListingDataStores(Logger);
        var result = await base.LoadItems(ct).ConfigureAwait(false);
        if (result.IsSuccess)
            DataStoreLog.DataStoresFound(Logger, result.Value?.Count ?? 0);
        return result;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
