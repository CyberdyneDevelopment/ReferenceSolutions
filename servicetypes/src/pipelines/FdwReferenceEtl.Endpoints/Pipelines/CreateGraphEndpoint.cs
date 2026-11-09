using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to create a graph pipeline.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateGraphEndpoint : CreateGraphEndpointBase
{
    /// <inheritdoc />
    public CreateGraphEndpoint(
        IPipelineConfigurationProvider provider, ITransformationConfigurationProvider transformations,
        IDataSetConfigurationProvider dataSets, PipelineOwner owner, ILogger<CreateGraphEndpoint> logger)
        : base(logger, provider, transformations, dataSets, owner)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
