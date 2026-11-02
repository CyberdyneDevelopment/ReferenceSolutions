using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to list quality rules.
/// Delegates fully to the FDW base implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListQualityRulesEndpoint : ListQualityRulesEndpointBase
{
    /// <inheritdoc />
    public ListQualityRulesEndpoint(IQualityRuleConfigurationProvider provider)
        : base(provider)
    {
    }
}
