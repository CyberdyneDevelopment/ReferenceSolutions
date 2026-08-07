using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Etl.Server.Logging;

namespace Reference.Etl.Server.Endpoints.Executions;

/// <summary>
/// Request for edge inspection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InspectEdgeRequest
{
    public Guid ExecutionId { get; set; }
    public Guid SourceTaskId { get; set; }
    public Guid TargetTaskId { get; set; }
}

/// <summary>
/// Edge inspector state response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EdgeInspectorDto
{
    /// <summary>Gets or sets total records that have flowed across this edge.</summary>
    public long RecordsFlowed { get; set; }

    /// <summary>Gets or sets count of samples evicted from the ring buffer due to byte cap.</summary>
    public long SamplesDiscarded { get; set; }

    /// <summary>Gets or sets whether the sample buffer reached its byte cap.</summary>
    public bool SampleBufferAtCapacity { get; set; }

    /// <summary>Gets or sets the sample ring buffer (up to byte budget).</summary>
    public IReadOnlyList<IDictionary<string, object?>> Samples { get; set; } = Array.Empty<IDictionary<string, object?>>();
}

/// <summary>
/// <c>GET /etl/executions/{executionId}/edges/{sourceTaskId}/{targetTaskId}/inspect</c> — inspect edge state.
///
/// Returns the count of records flowed and a sample ring buffer for the directed edge.
/// Returns 404 if the execution is not a registered test-mode execution.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InspectEdgeEndpoint : Endpoint<InspectEdgeRequest, EdgeInspectorDto>
{
    private readonly IPipelineExecutionInspector _inspector;
    private readonly ILogger<InspectEdgeEndpoint> _logger;

    public InspectEdgeEndpoint(IPipelineExecutionInspector inspector, ILogger<InspectEdgeEndpoint> logger)
    {
        _inspector = inspector;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<InspectEdgeEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("etl/executions/{executionId}/edges/{sourceTaskId}/{targetTaskId}/inspect");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Inspect an edge's in-flight state";
            s.Description = "Returns records-flowed count and sample records for a directed edge in a test-mode execution. " +
                             "Returns 404 for non-test (production) executions.";
        });
    }

    public override async Task HandleAsync(InspectEdgeRequest req, CancellationToken ct)
    {
        EtlServerLog.InspectEdgeRequestReceived(_logger, req.ExecutionId, req.SourceTaskId, req.TargetTaskId);

        // Why: 404 for non-test executions — production runs never expose sample data
        if (!_inspector.IsTestExecution(req.ExecutionId))
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var state = _inspector.GetEdgeState(req.ExecutionId, req.SourceTaskId, req.TargetTaskId);
        if (state is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new EdgeInspectorDto
        {
            RecordsFlowed = state.RecordsFlowed,
            SamplesDiscarded = state.SamplesDiscarded,
            SampleBufferAtCapacity = state.SampleBufferAtCapacity,
            Samples = state.Samples
        }, ct).ConfigureAwait(false);
    }
}
