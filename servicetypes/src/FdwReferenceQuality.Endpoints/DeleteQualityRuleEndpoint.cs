using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Endpoints;

namespace FdwReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to delete a quality rule.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteQualityRuleEndpoint : DeleteQualityRuleEndpointBase
{
    /// <inheritdoc />
    public DeleteQualityRuleEndpoint(IQualityRuleConfigurationProvider provider)
        : base(provider)
    {
    }
}
