using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Fdw.Operations.Endpoints.Executions;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to cancel an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class CancelOperationExecutionEndpoint : CancelExecutionEndpointBase
{
    private readonly ILogger<CancelOperationExecutionEndpoint> _logger;

    public CancelOperationExecutionEndpoint(IExecutionTracker tracker, ILogger<CancelOperationExecutionEndpoint> logger)
        : base(logger, tracker) {
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
