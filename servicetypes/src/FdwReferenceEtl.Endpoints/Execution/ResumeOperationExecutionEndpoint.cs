using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Fdw.Operations.Endpoints.Executions;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to resume a paused execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResumeOperationExecutionEndpoint : ResumeExecutionEndpointBase
{
    private readonly ILogger<ResumeOperationExecutionEndpoint> _logger;

    public ResumeOperationExecutionEndpoint(IExecutionTracker tracker, ILogger<ResumeOperationExecutionEndpoint> logger)
        : base(logger, tracker) {
        _logger = logger;
    }

    protected override void OnResumingExecution(Guid id)
    {
        ExecutionLog.ResumingExecution(_logger, id);
    }

    protected override void OnExecutionNotFound(Guid id)
    {
        ExecutionLog.ExecutionNotFound(_logger, id);
    }

    protected override void OnExecutionAlreadyTerminal(Guid id, string state)
    {
        ExecutionLog.ExecutionAlreadyTerminal(_logger, id, state);
    }

    protected override void OnResumeFailed(Guid id, string error)
    {
        ExecutionLog.ExecutionResumeFailed(_logger, id, error);
    }

    protected override void OnExecutionResumed(Guid id)
    {
        ExecutionLog.ExecutionResumed(_logger, id);
    }
}
