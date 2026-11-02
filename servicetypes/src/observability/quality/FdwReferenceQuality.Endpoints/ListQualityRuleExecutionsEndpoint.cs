using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint that lists the recorded executions of one quality rule, newest first (FDW-1363).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListQualityRuleExecutionsEndpoint : ListQualityRuleExecutionsEndpointBase
{
    /// <inheritdoc />
    public ListQualityRuleExecutionsEndpoint(IDataGatewayProvider dataGateways, ILogger<ListQualityRuleExecutionsEndpoint> logger)
        : base(logger, dataGateways)
    {
    }
}
