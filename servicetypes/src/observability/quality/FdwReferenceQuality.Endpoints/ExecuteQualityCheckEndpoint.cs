using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to execute a quality check for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecuteQualityCheckEndpoint : ExecuteQualityCheckEndpointBase
{
    /// <inheritdoc />
    public ExecuteQualityCheckEndpoint(IQualityRuleConfigurationProvider provider, IDataGatewayProvider dataGateways, ILogger<ExecuteQualityCheckEndpoint> logger)
        : base(logger, provider, dataGateways) {
    }
}
