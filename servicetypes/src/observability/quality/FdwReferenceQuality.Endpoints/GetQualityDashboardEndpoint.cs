using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to get the quality dashboard summary.
/// Delegates fully to the FDW base implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetQualityDashboardEndpoint : GetQualityDashboardEndpointBase
{
    /// <inheritdoc />
    public GetQualityDashboardEndpoint(IQualityRuleConfigurationProvider provider)
        : base(provider)
    {
    }
}
