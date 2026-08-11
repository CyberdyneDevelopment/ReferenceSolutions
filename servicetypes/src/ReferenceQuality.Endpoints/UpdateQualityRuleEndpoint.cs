using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to update a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateQualityRuleEndpoint : Fdw.Services.Quality.Endpoints.UpdateQualityRuleEndpoint
{
    /// <inheritdoc />
    public UpdateQualityRuleEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
