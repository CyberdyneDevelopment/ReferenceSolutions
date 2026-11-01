using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Scripts;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to list the stored SQL scripts.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListScriptsEndpoint : ListScriptsEndpointBase
{
    /// <inheritdoc />
    public ListScriptsEndpoint(ScriptConfigurationProvider provider, ILogger<ListScriptsEndpoint> logger)
        : base(logger, provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Scripts");
    }
}
