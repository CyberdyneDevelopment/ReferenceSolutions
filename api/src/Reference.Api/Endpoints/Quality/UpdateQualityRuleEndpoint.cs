using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to update a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateQualityRuleEndpoint : Fdw.Services.Quality.Endpoints.UpdateQualityRuleEndpoint
{
    /// <inheritdoc />
    public UpdateQualityRuleEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
