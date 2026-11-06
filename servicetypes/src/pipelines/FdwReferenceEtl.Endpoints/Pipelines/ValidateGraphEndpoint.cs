using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to check a graph pipeline draft without saving it.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ValidateGraphEndpoint : ValidateGraphEndpointBase
{
    /// <inheritdoc />
    public ValidateGraphEndpoint(
        ITransformationConfigurationProvider transformations, IDataSetConfigurationProvider dataSets, ILogger<ValidateGraphEndpoint> logger)
        : base(logger, transformations, dataSets)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
