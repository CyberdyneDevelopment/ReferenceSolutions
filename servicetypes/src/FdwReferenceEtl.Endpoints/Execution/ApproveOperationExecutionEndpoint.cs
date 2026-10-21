using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Endpoints.Executions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Projects.Execution;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// <c>POST /etl/executions/{Id}/approve</c>: lets a run that waits for approval start (FDW-1740). The run goes back to Scheduled and is
/// queued as the trigger would have queued it: a project run from the project name and trigger source its record holds, a pipeline run from
/// the request the trigger kept for it (FDW-1937). A pipeline run whose kept request is gone (the host restarted) is not queued; it goes
/// back to waiting and can be denied.
/// </summary>
[ExcludeFromCodeCoverage]
public class ApproveOperationExecutionEndpoint : ApproveExecutionEndpointBase
{
    private const string ProjectItemType = "Project";
    private const string ProjectNameParameter = "ProjectName";

    private readonly ILogger<ApproveOperationExecutionEndpoint> _logger;
    private readonly ProjectExecutionQueue _projectQueue;
    private readonly IPipelineExecutionQueue _pipelineQueue;
    private readonly IPipelineRunHolds _holds;

    /// <summary>Initializes a new instance of the <see cref="ApproveOperationExecutionEndpoint"/> class.</summary>
    /// <param name="tracker">Reads and moves the executions.</param>
    /// <param name="projectQueue">The queue a project run is started from.</param>
    /// <param name="pipelineQueue">The queue a pipeline run is started from.</param>
    /// <param name="holds">Keeps the requests of the pipeline runs that wait for approval.</param>
    /// <param name="logger">The logger.</param>
    public ApproveOperationExecutionEndpoint(
        IExecutionTracker tracker,
        ProjectExecutionQueue projectQueue,
        IPipelineExecutionQueue pipelineQueue,
        IPipelineRunHolds holds,
        ILogger<ApproveOperationExecutionEndpoint> logger)
        : base(logger, tracker)
    {
        _logger = logger;
        _projectQueue = projectQueue;
        _pipelineQueue = pipelineQueue;
        _holds = holds;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Release(IExecutionItem execution, CancellationToken ct)
    {
        if (string.Equals(execution.ItemType.Name, ExecutionItemTypes.Job.Name, StringComparison.Ordinal))
        {
            return await ReleasePipelineRun(execution, ct).ConfigureAwait(false);
        }

        if (!string.Equals(execution.ItemType.Name, ProjectItemType, StringComparison.Ordinal))
        {
            return GenericResult.Failure(ExecutionLog.ExecutionNotAProjectRun(_logger, execution.Id, execution.ItemType.Name));
        }

        if (!execution.Parameters.TryGetValue(ProjectNameParameter, out var name) || name is not string { Length: > 0 } projectName)
        {
            return GenericResult.Failure(ExecutionLog.ProjectRunIncomplete(_logger, execution.Id, "project name"));
        }

        if (execution.TriggerSource is not { Length: > 0 } triggerSource)
        {
            return GenericResult.Failure(ExecutionLog.ProjectRunIncomplete(_logger, execution.Id, "trigger source"));
        }

        var enqueued = await _projectQueue.Enqueue(
            new ProjectExecutionRequest { ExecutionId = execution.Id, ProjectName = projectName, TriggerSource = triggerSource }, ct).ConfigureAwait(false);
        return enqueued
            ? GenericResult.Success()
            : GenericResult.Failure(ExecutionLog.ProjectQueueFullOnRelease(_logger, execution.Id, projectName));
    }

    // The request the trigger kept for the run is queued as it was; if the queue is full it is kept again, so the decision can be made again.
    private async Task<IGenericResult> ReleasePipelineRun(IExecutionItem execution, CancellationToken ct)
    {
        var request = _holds.Take(execution.Id);
        if (request is null)
        {
            return GenericResult.Failure(ExecutionLog.PipelineRunHoldLost(_logger, execution.Id));
        }

        if (await _pipelineQueue.Enqueue(request, ct).ConfigureAwait(false))
        {
            return GenericResult.Success();
        }

        _ = _holds.Hold(request);
        return GenericResult.Failure(ExecutionLog.PipelineQueueFullOnRelease(_logger, execution.Id, request.PipelineName));
    }

    /// <inheritdoc />
    protected override void OnCallerUnknown(Guid id, string decision) => ExecutionLog.DecisionCallerUnknown(_logger, id, decision);

    /// <inheritdoc />
    protected override void OnDeciding(Guid id, string decision, string actor) => ExecutionLog.DecidingExecution(_logger, id, decision, actor);

    /// <inheritdoc />
    protected override void OnExecutionNotFound(Guid id) => ExecutionLog.ExecutionNotFound(_logger, id);

    /// <inheritdoc />
    protected override void OnNotAwaitingApproval(Guid id, string state) => ExecutionLog.ExecutionNotAwaitingApproval(_logger, id, state);

    /// <inheritdoc />
    protected override void OnDecisionRefused(Guid id, string? error) => ExecutionLog.ExecutionDecisionRefused(_logger, id, error);

    /// <inheritdoc />
    protected override void OnReleaseFailed(Guid id, string? error) => ExecutionLog.ExecutionReleaseFailed(_logger, id, error);

    /// <inheritdoc />
    protected override void OnReturnToWaitingFailed(Guid id, string? error) => ExecutionLog.ExecutionReturnToWaitingFailed(_logger, id, error);

    /// <inheritdoc />
    protected override void OnDecided(Guid id, string decision, string actor) => ExecutionLog.ExecutionDecided(_logger, id, decision, actor);
}
