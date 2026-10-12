using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Web.Search.Endpoints;

namespace FdwReferenceData.Endpoints;

/// <summary>
/// Concrete endpoint for global search across entities.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SearchEndpoint : SearchEndpointBase
{
    /// <inheritdoc />
    public SearchEndpoint(
        IConfigurationGatewayProvider configurationGateways,
        ConnectionConfigurationProvider configProvider,
        ILogger<Fdw.Web.Search.Endpoints.SearchEndpointBase> logger)
        : base(configurationGateways, configProvider, logger)
    {
    }
}
