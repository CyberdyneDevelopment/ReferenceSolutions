using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Etl.Scripts;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to run a SQL script draft once, without saving it.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class RunDraftScriptEndpoint : RunDraftScriptEndpointBase
{
    /// <inheritdoc />
    public RunDraftScriptEndpoint(IScriptService scripts, ILogger<RunDraftScriptEndpoint> logger)
        : base(logger, scripts)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Scripts");
    }
}
