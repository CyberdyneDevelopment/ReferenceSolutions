using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to execute all quality checks for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteAllQualityChecksEndpoint : Fdw.Services.Quality.Endpoints.ExecuteAllQualityChecksEndpoint
{
    /// <inheritdoc />
    public ExecuteAllQualityChecksEndpoint(
        QualityConfigurationProvider provider,
        IDataGateway dataGateway)
        : base(provider, dataGateway)
    {
    }
}
