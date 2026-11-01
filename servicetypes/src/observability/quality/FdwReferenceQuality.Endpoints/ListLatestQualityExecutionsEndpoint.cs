using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint that answers the latest recorded execution of each quality rule of a data set (FDW-1363).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListLatestQualityExecutionsEndpoint : ListLatestQualityExecutionsEndpointBase
{
    /// <inheritdoc />
    public ListLatestQualityExecutionsEndpoint(IQualityRuleConfigurationProvider provider, IDataGatewayProvider dataGateways, ILogger<ListLatestQualityExecutionsEndpoint> logger)
        : base(logger, provider, dataGateways)
    {
    }
}
