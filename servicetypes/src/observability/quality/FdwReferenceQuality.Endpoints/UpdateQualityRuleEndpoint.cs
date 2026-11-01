using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to update a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateQualityRuleEndpoint : UpdateQualityRuleEndpointBase
{
    /// <inheritdoc />
    public UpdateQualityRuleEndpoint(IQualityRuleConfigurationProvider provider)
        : base(provider)
    {
    }
}
