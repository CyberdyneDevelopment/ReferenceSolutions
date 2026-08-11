using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// Concrete endpoint to execute a quality check for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecuteQualityCheckEndpoint : Fdw.Services.Quality.Endpoints.ExecuteQualityCheckEndpoint
{
    /// <inheritdoc />
    public ExecuteQualityCheckEndpoint(
        QualityConfigurationProvider provider,
        IDataGateway dataGateway)
        : base(provider, dataGateway)
    {
    }
}
