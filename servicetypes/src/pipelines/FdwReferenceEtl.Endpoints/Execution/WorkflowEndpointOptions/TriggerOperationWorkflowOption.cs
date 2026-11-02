using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.WorkflowEndpointOptions;

/// <summary>The TriggerWorkflow endpoint.</summary>
[WebMcpTool("trigger_workflow", "Trigger workflow.")]
[TypeOption(typeof(WorkflowEndpoints), "TriggerWorkflow")]
public class TriggerOperationWorkflowOption : WorkflowEndpointBase<TriggerOperationWorkflowEndpoint>
{
}
