using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;

namespace FdwReferenceData.Endpoints;

/// <summary>
/// Concrete endpoint to get dataset lineage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetLineageEndpoint : GetDataSetLineageEndpointBase
{
    /// <inheritdoc />
    public GetDataSetLineageEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        Fdw.Services.Pipelines.PipelineServiceConfigurationProvider pipelineProvider,
        Fdw.Services.Calculations.ICalculationConfigurationProvider calculations,
        ILogger<Fdw.Operations.Endpoints.GetDataSetLineageEndpointBase> logger)
        : base(dataSetProvider, pipelineProvider, calculations, logger)
    {
    }
}
