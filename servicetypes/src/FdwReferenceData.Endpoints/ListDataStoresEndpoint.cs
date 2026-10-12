using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list DataStores, one page at a time.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStoresEndpoint : ListDataStoresEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataStoresEndpoint"/> class.
    /// </summary>
    public ListDataStoresEndpoint(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        IAuthorizationService authorization,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<ListDataStoresEndpoint> logger)
        : base(logger, dataStoreProvider, connectionProvider, authorization, configurationGateways)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
