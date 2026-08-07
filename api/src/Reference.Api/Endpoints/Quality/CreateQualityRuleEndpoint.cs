using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to create a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateQualityRuleEndpoint : Fdw.Services.Quality.Endpoints.CreateQualityRuleEndpoint
{
    /// <inheritdoc />
    public CreateQualityRuleEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
