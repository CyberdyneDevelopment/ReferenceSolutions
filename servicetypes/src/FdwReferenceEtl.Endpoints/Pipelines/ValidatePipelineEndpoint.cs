using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint that checks a pipeline draft without saving it. Route: POST /pipelines/validate.
/// </summary>
/// <remarks>
/// Why this exists: the base is abstract and FastEndpoints only registers concrete endpoints, so without this
/// closure the route 404s and the Pipeline Builder cannot show a draft's problems before saving (FDW-1095). It
/// takes the same dependencies as <see cref="CreatePipelineEndpoint"/> because it answers the same questions.
/// </remarks>
[ExcludeFromCodeCoverage]
public class ValidatePipelineEndpoint : ValidatePipelineEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ValidatePipelineEndpoint"/> class.</summary>
    /// <param name="provider">Resolves existing pipelines, to find a name that is already taken.</param>
    /// <param name="dataSets">Resolves data sets as the caller.</param>
    /// <param name="connections">Resolves connections as the caller.</param>
    /// <param name="dataStoreConfigurations">Resolves the data stores an Execute load step's container is read from.</param>
    /// <param name="transformations">Resolves the named Transformations a field mapping's chain applies.</param>
    /// <param name="logger">The logger.</param>
    public ValidatePipelineEndpoint(
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations,
        ILogger<ValidatePipelineEndpoint> logger)
        : base(logger, provider, dataSets, connections, dataStoreConfigurations, transformations)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
