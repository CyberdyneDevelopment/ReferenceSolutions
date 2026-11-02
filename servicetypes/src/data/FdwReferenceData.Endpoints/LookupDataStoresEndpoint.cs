using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to look up data stores by name, one page at a time, for pickers and name-taken checks (FDW-1194).
/// </summary>
[ExcludeFromCodeCoverage]
public class LookupDataStoresEndpoint : LookupDataStoresEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupDataStoresEndpoint"/> class.</summary>
    public LookupDataStoresEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<LookupDataStoresEndpoint> logger)
        : base(logger, dataStoreProvider, configurationGateways)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
