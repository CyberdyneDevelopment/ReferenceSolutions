using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to execute all quality checks for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecuteAllQualityChecksEndpoint : ExecuteAllQualityChecksEndpointBase
{
    /// <inheritdoc />
    public ExecuteAllQualityChecksEndpoint(IQualityRuleConfigurationProvider provider, IDataGatewayProvider dataGateways, ILogger<ExecuteAllQualityChecksEndpoint> logger)
        : base(logger, provider, dataGateways) {
    }
}
