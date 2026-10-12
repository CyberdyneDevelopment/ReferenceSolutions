using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to look up pipelines by name, one page at a time, for pickers and name-taken checks (FDW-1194).
/// </summary>
[ExcludeFromCodeCoverage]
public class LookupPipelinesEndpoint : LookupPipelinesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupPipelinesEndpoint"/> class.</summary>
    public LookupPipelinesEndpoint(PipelineServiceConfigurationProvider configProvider, ILogger<LookupPipelinesEndpoint> logger)
        : base(logger, configProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Pipelines");
    }
}
