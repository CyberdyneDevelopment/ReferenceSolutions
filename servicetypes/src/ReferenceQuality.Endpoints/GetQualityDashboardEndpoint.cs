using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to get the quality dashboard summary.
/// Delegates fully to the FDW base implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetQualityDashboardEndpoint : Fdw.Services.Quality.Endpoints.GetQualityDashboardEndpoint
{
    /// <inheritdoc />
    public GetQualityDashboardEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
