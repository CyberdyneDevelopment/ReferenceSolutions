using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;

namespace FdwReferenceData.Endpoints;

/// <summary>
/// Concrete endpoint to get impact analysis for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetImpactAnalysisEndpoint : GetImpactAnalysisEndpointBase
{
    /// <inheritdoc />
    public GetImpactAnalysisEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        CategoryConfigurationProvider categories,
        ILogger<Fdw.Operations.Endpoints.GetImpactAnalysisEndpointBase> logger)
        : base(dataSetProvider, categories, logger)
    {
    }
}
