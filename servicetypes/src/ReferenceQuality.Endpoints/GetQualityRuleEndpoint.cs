using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to get a quality rule by ID.
/// Delegates fully to the FDW base implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetQualityRuleEndpoint : Fdw.Services.Quality.Endpoints.GetQualityRuleEndpoint
{
    /// <inheritdoc />
    public GetQualityRuleEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
