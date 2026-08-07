using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to resume a paused execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class ResumeExecutionEndpoint : Fdw.Operations.Endpoints.Executions.ResumeExecutionEndpoint
{
    private readonly ILogger<ResumeExecutionEndpoint> _logger;

    public ResumeExecutionEndpoint(IExecutionTracker tracker, ILogger<ResumeExecutionEndpoint> logger)
        : base(tracker)
    {
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
