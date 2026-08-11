using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to list quality rules.
/// Delegates fully to the FDW base implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListQualityRulesEndpoint : Fdw.Services.Quality.Endpoints.ListQualityRulesEndpoint
{
    /// <inheritdoc />
    public ListQualityRulesEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
