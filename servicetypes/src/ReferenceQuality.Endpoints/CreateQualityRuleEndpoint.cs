using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to create a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateQualityRuleEndpoint : Fdw.Services.Quality.Endpoints.CreateQualityRuleEndpoint
{
    /// <inheritdoc />
    public CreateQualityRuleEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
