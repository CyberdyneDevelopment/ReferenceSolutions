using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Endpoints.Executions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// <c>POST /etl/executions/{Id}/deny</c>: refuses a run that waits for approval (FDW-1740). The run ends Cancelled and its state-change
/// event records who denied it and the optional reason; the request kept for a pipeline run is discarded (FDW-1937).
/// </summary>
[ExcludeFromCodeCoverage]
public class DenyOperationExecutionEndpoint : DenyExecutionEndpointBase
{
    private readonly ILogger<DenyOperationExecutionEndpoint> _logger;
    private readonly IPipelineRunHolds _holds;

    /// <summary>Initializes a new instance of the <see cref="DenyOperationExecutionEndpoint"/> class.</summary>
    /// <param name="tracker">Reads and moves the executions.</param>
    /// <param name="holds">Keeps the requests of the pipeline runs that wait for approval.</param>
    /// <param name="logger">The logger.</param>
    public DenyOperationExecutionEndpoint(IExecutionTracker tracker, IPipelineRunHolds holds, ILogger<DenyOperationExecutionEndpoint> logger)
        : base(logger, tracker)
    {
        _logger = logger;
        _holds = holds;
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Release(IExecutionItem execution, CancellationToken ct)
    {
        // A denied run never runs, so the request kept for it is discarded; none is kept for a project run, and none is left after a restart.
        _ = _holds.Take(execution.Id);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
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
    protected override void OnDecided(Guid id, string decision, string actor) => ExecutionLog.ExecutionDecided(_logger, id, decision, actor);
}
