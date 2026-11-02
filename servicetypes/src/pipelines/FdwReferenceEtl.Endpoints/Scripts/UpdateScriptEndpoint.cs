using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to replace a stored SQL script.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateScriptEndpoint : UpdateScriptEndpointBase
{
    /// <inheritdoc />
    public UpdateScriptEndpoint(ScriptConfigurationProvider provider, ILogger<UpdateScriptEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Scripts");
    }
}
