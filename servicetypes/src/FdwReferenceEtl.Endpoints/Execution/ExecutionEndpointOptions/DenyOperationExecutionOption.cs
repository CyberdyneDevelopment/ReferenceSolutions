using Fdw.Collections.Attributes;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>Serves the deny endpoint for a run that waits for approval (FDW-1740). Not offered as an agent tool: a decision is a person's.</summary>
[TypeOption(typeof(ExecutionEndpoints), "DenyExecution")]
public class DenyOperationExecutionOption : ExecutionEndpointBase<DenyOperationExecutionEndpoint>
{
}
