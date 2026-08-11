using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Services.Etl.Projects.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceEtlJobs.Endpoints.Logging;

namespace ReferenceEtlExecutions.Endpoints;

/// <summary>
/// Request for cancellation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CancelExecutionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>DELETE /etl/executions/{id}</c> — cancel an in-progress execution.
///
/// Works for both project and pipeline executions by resolving the type from the tracker item.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CancelExecutionEndpoint : Endpoint<CancelExecutionRequest>
{
    private readonly IExecutionTracker _executionTracker;
    private readonly IOrchestrationNodeOrchestrator _orchestrator;
    private readonly ILogger<CancelExecutionEndpoint> _logger;

    public CancelExecutionEndpoint(
        IExecutionTracker executionTracker,
        IOrchestrationNodeOrchestrator orchestrator,
        ILogger<CancelExecutionEndpoint> logger)
    {
        _executionTracker = executionTracker;
        _orchestrator = orchestrator;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<CancelExecutionEndpoint>.Instance;
    }

    public override void Configure()
    {
        Delete("etl/executions/{id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Cancel an execution";
            s.Description = "Cancels a running project or pipeline execution.";
        });
    }

    public override async Task HandleAsync(CancelExecutionRequest req, CancellationToken ct)
    {
        EtlServerLog.CancelExecutionRequestReceived(_logger, req.Id);

        var itemResult = await _executionTracker.GetItem(req.Id, ct).ConfigureAwait(false);
        if (!itemResult.IsSuccess || itemResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Why: complete with Cancelled status regardless of execution type — tracker handles the state machine.
        var completeResult = await _executionTracker.Complete(
            req.Id, false, "Cancelled", "Cancelled by user request",
            CancellationToken.None).ConfigureAwait(false);

        if (!completeResult.IsSuccess)
        {
            AddError(EtlServerLog.GetError(completeResult, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        EtlServerLog.ExecutionCancelled(_logger, req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
