using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.Extensions.Logging;
using ReferenceExecution.Endpoints.Logging;

namespace ReferenceExecution.Endpoints;

/// <summary>
/// Endpoint to trigger a workflow execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class TriggerWorkflowEndpoint : Fdw.Operations.Endpoints.Executions.TriggerWorkflowEndpoint
{
    private readonly ILogger<TriggerWorkflowEndpoint> _logger;

    public TriggerWorkflowEndpoint(ILogger<TriggerWorkflowEndpoint> logger)
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
