using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to get a quality rule by ID.
/// Delegates fully to the FDW base implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetQualityRuleEndpoint : GetQualityRuleEndpointBase
{
    /// <inheritdoc />
    public GetQualityRuleEndpoint(IQualityRuleConfigurationProvider provider)
        : base(provider)
    {
    }
}
