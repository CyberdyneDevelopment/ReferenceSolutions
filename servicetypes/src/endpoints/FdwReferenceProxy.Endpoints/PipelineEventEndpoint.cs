using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Receives pipeline events relayed from a host that serves no hub (the ETL host) and broadcasts
/// them to the hub groups through this host's own broadcaster.
/// POST /api/v1/pipelines/events
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PipelineEventEndpoint : PipelineEventEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineEventEndpoint"/> class.</summary>
    public PipelineEventEndpoint(IPipelineStatusBroadcaster broadcaster, ILogger<PipelineEventEndpoint> logger)
        : base(broadcaster, logger ?? NullLogger<PipelineEventEndpoint>.Instance)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Relay a pipeline event to the hub";
            s.Description = "Receives a pipeline event from a host that serves no hub and broadcasts it to the pipeline hub groups.";
        });
        Tags("Pipelines");
    }
}
