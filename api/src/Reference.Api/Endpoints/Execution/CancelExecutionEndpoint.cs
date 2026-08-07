using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to cancel an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class CancelExecutionEndpoint : Fdw.Operations.Endpoints.Executions.CancelExecutionEndpoint
{
    private readonly ILogger<CancelExecutionEndpoint> _logger;

    public CancelExecutionEndpoint(IExecutionTracker tracker, ILogger<CancelExecutionEndpoint> logger)
        : base(tracker)
    {
        _logger = logger;
    }

    protected override void OnCancellingExecution(Guid id)
    {
        ExecutionLog.CancellingExecution(_logger, id);
    }

    protected override void OnExecutionNotFound(Guid id)
    {
        ExecutionLog.ExecutionNotFound(_logger, id);
    }

    protected override void OnExecutionAlreadyTerminal(Guid id, string state)
    {
        ExecutionLog.ExecutionAlreadyTerminal(_logger, id, state);
    }

    protected override void OnCancelFailed(Guid id, string error)
    {
        ExecutionLog.ExecutionCancelFailed(_logger, id, error);
    }

    protected override void OnExecutionCancelled(Guid id)
    {
        ExecutionLog.ExecutionCancelled(_logger, id);
    }
}
