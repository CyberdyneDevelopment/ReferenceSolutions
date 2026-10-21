using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to run a stored SQL script once with the values given.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class RunScriptEndpoint : RunScriptEndpointBase
{
    /// <inheritdoc />
    public RunScriptEndpoint(ScriptConfigurationProvider provider, IScriptService scripts, ILogger<RunScriptEndpoint> logger)
        : base(logger, provider, scripts)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Scripts");
    }
}
