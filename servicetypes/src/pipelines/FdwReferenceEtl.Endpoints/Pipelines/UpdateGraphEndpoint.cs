using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to replace a graph pipeline whole.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateGraphEndpoint : UpdateGraphEndpointBase
{
    /// <inheritdoc />
    public UpdateGraphEndpoint(
        PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations,
        IDataSetConfigurationProvider dataSets, ILogger<UpdateGraphEndpoint> logger)
        : base(logger, provider, transformations, dataSets)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
