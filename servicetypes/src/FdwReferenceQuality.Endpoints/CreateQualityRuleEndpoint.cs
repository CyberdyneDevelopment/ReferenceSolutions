using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to create a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateQualityRuleEndpoint : CreateQualityRuleEndpointBase
{
    /// <inheritdoc />
    public CreateQualityRuleEndpoint(IQualityRuleConfigurationProvider provider)
        : base(provider)
    {
    }
}
