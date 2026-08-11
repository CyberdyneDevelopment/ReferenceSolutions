using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Concrete endpoint to get impact analysis for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetImpactAnalysisEndpoint : Fdw.Operations.Endpoints.GetImpactAnalysisEndpoint
{
    /// <inheritdoc />
    public GetImpactAnalysisEndpoint(
        IConfigurationGateway configurationGateway,
        ILogger<Fdw.Operations.Endpoints.GetImpactAnalysisEndpoint> logger)
        : base(configurationGateway, logger)
    {
    }
}
