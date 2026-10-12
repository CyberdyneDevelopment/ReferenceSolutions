using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.Extensions.Logging;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Endpoints.Executions;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to trigger a workflow execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class TriggerOperationWorkflowEndpoint : TriggerWorkflowEndpointBase
{
    private readonly ILogger<TriggerOperationWorkflowEndpoint> _logger;

    public TriggerOperationWorkflowEndpoint(ILogger<TriggerOperationWorkflowEndpoint> logger, IExecutionTracker tracker)
        : base(logger, tracker)
    {
        _logger = logger;
    }

    protected override void OnDryRun(string name, string correlationId)
    {
        ExecutionLog.DryRunRequested(_logger, name);
    }

    protected override void OnTriggerFailed(string name, string error)
    {
        ExecutionLog.WorkflowTriggerFailed(_logger, name, error);
    }

    protected override void OnTriggerAccepted(string name, Guid executionId, string correlationId)
    {
        ExecutionLog.TriggeringWorkflow(_logger, name, correlationId);
        ExecutionLog.WorkflowTriggered(_logger, name, executionId);
    }
}
