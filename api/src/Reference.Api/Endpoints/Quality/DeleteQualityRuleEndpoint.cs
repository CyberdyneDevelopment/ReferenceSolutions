using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to delete a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteQualityRuleEndpoint : Fdw.Services.Quality.Endpoints.DeleteQualityRuleEndpoint
{
    /// <inheritdoc />
    public DeleteQualityRuleEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
