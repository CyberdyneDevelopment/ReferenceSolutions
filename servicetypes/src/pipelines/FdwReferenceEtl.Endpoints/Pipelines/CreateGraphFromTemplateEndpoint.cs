using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to make a graph pipeline from a template.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateGraphFromTemplateEndpoint : CreateGraphFromTemplateEndpointBase
{
    /// <inheritdoc />
    public CreateGraphFromTemplateEndpoint(
        PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations,
        IDataSetConfigurationProvider dataSets, ILogger<CreateGraphFromTemplateEndpoint> logger)
        : base(logger, provider, transformations, dataSets)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
