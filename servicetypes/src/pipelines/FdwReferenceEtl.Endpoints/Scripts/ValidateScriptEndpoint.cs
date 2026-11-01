using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to check a stored SQL script draft without saving it.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ValidateScriptEndpoint : ValidateScriptEndpointBase
{
    /// <inheritdoc />
    public ValidateScriptEndpoint(ILogger<ValidateScriptEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Scripts");
    }
}
