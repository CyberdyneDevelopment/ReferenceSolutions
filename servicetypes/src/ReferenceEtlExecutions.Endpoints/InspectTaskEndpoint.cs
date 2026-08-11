using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlExecutions.Endpoints;

/// <summary>
/// Request for task inspection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InspectTaskRequest
{
    public Guid ExecutionId { get; set; }
    public Guid TaskId { get; set; }
}

/// <summary>
/// Task inspector state response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TaskInspectorDto
{
    /// <summary>Gets or sets total records received by this task node.</summary>
    public long RecordsIn { get; set; }

    /// <summary>Gets or sets total records emitted on the data stream.</summary>
    public long RecordsOut { get; set; }

    /// <summary>Gets or sets total records routed to a reject/error stream.</summary>
    public long RecordsDiscarded { get; set; }

    /// <summary>Gets or sets current records being processed in the active batch.</summary>
    public long RecordsHeld { get; set; }

    /// <summary>Gets or sets count of samples evicted from the ring buffer due to byte cap.</summary>
    public long SamplesDiscarded { get; set; }

    /// <summary>Gets or sets whether the sample buffer reached its byte cap.</summary>
    public bool SampleBufferAtCapacity { get; set; }

    /// <summary>Gets or sets the sample ring buffer (up to byte budget).</summary>
    public IReadOnlyList<IDictionary<string, object?>> Samples { get; set; } = Array.Empty<IDictionary<string, object?>>();
}

/// <summary>
/// <c>GET /etl/executions/{executionId}/tasks/{taskId}/inspect</c> — inspect task node state.
///
/// Returns counters and a sample ring buffer of recently processed records.
/// Returns 404 if the execution is not a registered test-mode execution.
/// Production executions never retain samples — no PII leakage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InspectTaskEndpoint : Endpoint<InspectTaskRequest, TaskInspectorDto>
{
    private readonly IPipelineExecutionInspector _inspector;
    private readonly ILogger<InspectTaskEndpoint> _logger;

    public InspectTaskEndpoint(IPipelineExecutionInspector inspector, ILogger<InspectTaskEndpoint> logger)
    {
        _inspector = inspector;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<InspectTaskEndpoint>.Instance;
    }

    public override void Configure()
    {
        Get("etl/executions/{executionId}/tasks/{taskId}/inspect");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Inspect a task node's in-flight state";
            s.Description = "Returns counters and sample records for a task node in a test-mode execution. " +
                             "Returns 404 for non-test (production) executions to prevent PII leakage.";
        });
    }

    public override async Task HandleAsync(InspectTaskRequest req, CancellationToken ct)
    {
        EtlServerLog.InspectTaskRequestReceived(_logger, req.ExecutionId, req.TaskId);

        // Why: 404 for non-test executions — production runs never expose sample data
        if (!_inspector.IsTestExecution(req.ExecutionId))
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var state = _inspector.GetTaskState(req.ExecutionId, req.TaskId);
        if (state is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new TaskInspectorDto
        {
            RecordsIn = state.RecordsIn,
            RecordsOut = state.RecordsOut,
            RecordsDiscarded = state.RecordsDiscarded,
            RecordsHeld = state.RecordsHeld,
            SamplesDiscarded = state.SamplesDiscarded,
            SampleBufferAtCapacity = state.SampleBufferAtCapacity,
            Samples = state.Samples
        }, ct).ConfigureAwait(false);
    }
}
