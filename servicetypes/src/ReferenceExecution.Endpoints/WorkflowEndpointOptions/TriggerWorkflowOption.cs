using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.WorkflowEndpointOptions;

/// <summary>The TriggerWorkflow endpoint.</summary>
[TypeOption(typeof(WorkflowEndpoints), "TriggerWorkflow")]
public class TriggerWorkflowOption : WorkflowEndpointBase<TriggerWorkflowEndpoint>
{
}
