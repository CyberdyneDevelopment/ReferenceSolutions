using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Etl.Server.Endpoints.Trigger;
using Reference.Etl.Server.Logging;

namespace Reference.Etl.Server.Endpoints.Executions;

/// <summary>
/// Request for the approve endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ApproveExecutionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>POST /etl/executions/{id}/approve</c> — approve an execution that is waiting for manual approval.
///
/// Transitions from AwaitingApproval to Triggered and enqueues the project execution.
/// Returns 400 if the execution is not in the AwaitingApproval state.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ApproveExecutionEndpoint : Endpoint<ApproveExecutionRequest, UnifiedTriggerResponse>
{
    private readonly IExecutionTracker _executionTracker;
    private readonly ProjectExecutionQueue _projectQueue;
    private readonly ILogger<ApproveExecutionEndpoint> _logger;

    public ApproveExecutionEndpoint(
        IExecutionTracker executionTracker,
        ProjectExecutionQueue projectQueue,
        ILogger<ApproveExecutionEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _projectQueue = projectQueue;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<ApproveExecutionEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/executions/{id}/approve");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Approve a pending execution";
            s.Description = "Approves an execution in AwaitingApproval state, transitioning it to Triggered and enqueuing it.";
        });
    }

    public override async Task HandleAsync(ApproveExecutionRequest req, CancellationToken ct)
    {
        EtlServerLog.ApproveExecutionRequestReceived(_logger, req.Id);

        var itemResult = await _executionTracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var item = itemResult.Value;

        // Why: only executions in AwaitingApproval can be approved — others are rejected
        if (!string.Equals(item.State.Name, "AwaitingApproval", StringComparison.Ordinal))
        {
            EtlServerLog.ExecutionNotAwaitingApproval(_logger, req.Id);
            AddError($"Execution {req.Id} is in state '{item.State.Name}', not 'AwaitingApproval'. Cannot approve.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        // Transition to Triggered
        var transitionResult = await _executionTracker.TransitionState(
            req.Id,
            ExecutionStateTypes.Triggered,
            message: "Approved by user request",
            actor: null,
            cancellationToken: ct).ConfigureAwait(false);

        if (!transitionResult.IsSuccess)
        {
            AddError(EtlServerLog.GetError(transitionResult, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        // Resolve the project name from execution parameters to enqueue
        var projectName = item.Name;

        var enqueued = await _projectQueue.Enqueue(new ProjectExecutionRequest
        {
            ExecutionId = req.Id,
            ProjectName = projectName,
            TriggerSource = "Approved"
        }, ct).ConfigureAwait(false);

        if (!enqueued)
        {
            EtlServerLog.UnifiedTriggerQueueFull(_logger, "project", projectName);
            await Send.ResponseAsync(new UnifiedTriggerResponse
            {
                ExecutionId = req.Id,
                Status = "QueueFull"
            }, 503, ct).ConfigureAwait(false);
            return;
        }

        EtlServerLog.ExecutionApprovedAndEnqueued(_logger, req.Id);
        await Send.OkAsync(new UnifiedTriggerResponse
        {
            ExecutionId = req.Id,
            Status = "Queued"
        }, ct).ConfigureAwait(false);
    }
}
