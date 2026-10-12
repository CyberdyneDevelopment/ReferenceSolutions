using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint that checks an edit of a saved pipeline without saving it. Route: POST /pipelines/{name}/validate.
/// </summary>
/// <remarks>
/// Why this exists: the base is abstract and FastEndpoints only registers concrete endpoints, so without this closure the
/// route 404s and the Pipeline Builder cannot show an edit's problems before saving (FDW-1105). It takes the dependencies
/// of <see cref="UpdatePipelineEndpoint"/> because it answers the same questions.
/// </remarks>
[ExcludeFromCodeCoverage]
public class ValidateExistingPipelineEndpoint : ValidateExistingPipelineEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ValidateExistingPipelineEndpoint"/> class.</summary>
    /// <param name="provider">Reads the saved pipeline the edit is about.</param>
    /// <param name="dataSets">Resolves data sets as the caller.</param>
    /// <param name="connections">Resolves connections as the caller.</param>
    /// <param name="dataStoreConfigurations">Resolves the data stores an Execute load step's container is read from.</param>
    /// <param name="transformations">Resolves the named Transformations a field mapping's chain applies.</param>
    /// <param name="logger">The logger.</param>
    public ValidateExistingPipelineEndpoint(
        PipelineServiceConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        IConnectionConfigurationProvider connections,
        DataStoreConfigurationProvider dataStoreConfigurations,
        ITransformationConfigurationProvider transformations,
        ILogger<ValidateExistingPipelineEndpoint> logger)
        : base(logger, provider, dataSets, connections, dataStoreConfigurations, transformations)
    {
    }

    /// <inheritdoc />
    protected override IGenericResult CheckApplicable(UpdatePipelineRequest request, IPipelineImplementationConfiguration original)
    {
        var wired = WiredEngine.Of(original, EndpointLogger);
        if (wired.IsFailure)
        {
            return wired;
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
