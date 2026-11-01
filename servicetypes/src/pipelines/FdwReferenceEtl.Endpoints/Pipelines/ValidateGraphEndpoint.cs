using System.Diagnostics.CodeAnalysis;
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
    public ValidateGraphEndpoint(ITransformationConfigurationProvider transformations, ILogger<ValidateGraphEndpoint> logger)
        : base(logger, transformations)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
