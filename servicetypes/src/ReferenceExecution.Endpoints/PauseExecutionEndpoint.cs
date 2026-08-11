using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using ReferenceExecution.Endpoints.Logging;

namespace ReferenceExecution.Endpoints;

/// <summary>
/// Endpoint to pause an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class PauseExecutionEndpoint : Fdw.Operations.Endpoints.Executions.PauseExecutionEndpoint
{
    private readonly ILogger<PauseExecutionEndpoint> _logger;

    public PauseExecutionEndpoint(IExecutionTracker tracker, ILogger<PauseExecutionEndpoint> logger)
        : base(tracker)
    {
        _logger = logger;
    }

    protected override void OnPausingExecution(Guid id)
    {
        ExecutionLog.PausingExecution(_logger, id);
    }

    protected override void OnExecutionNotFound(Guid id)
    {
        ExecutionLog.ExecutionNotFound(_logger, id);
    }

    protected override void OnExecutionAlreadyTerminal(Guid id, string state)
    {
        ExecutionLog.ExecutionAlreadyTerminal(_logger, id, state);
    }

    protected override void OnPauseFailed(Guid id, string error)
    {
        ExecutionLog.ExecutionPauseFailed(_logger, id, error);
    }

    protected override void OnExecutionPaused(Guid id)
    {
        ExecutionLog.ExecutionPaused(_logger, id);
    }
}
