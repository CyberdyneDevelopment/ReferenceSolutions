using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to create a stored SQL script.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateScriptEndpoint : CreateScriptEndpointBase
{
    /// <inheritdoc />
    public CreateScriptEndpoint(ScriptConfigurationProvider provider, ILogger<CreateScriptEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Scripts");
    }
}
