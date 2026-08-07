using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint for global search across entities.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SearchEndpoint : Fdw.Web.Search.Endpoints.SearchEndpoint
{
    /// <inheritdoc />
    public SearchEndpoint(
        IDataGateway dataGateway,
        ConnectionConfigurationProvider configProvider,
        ILogger<Fdw.Web.Search.Endpoints.SearchEndpoint> logger)
        : base(dataGateway, configProvider, logger)
    {
    }
}
