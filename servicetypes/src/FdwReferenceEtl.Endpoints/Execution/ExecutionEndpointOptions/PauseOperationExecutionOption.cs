using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The PauseExecution endpoint.</summary>
[WebMcpTool("pause_execution", "Pause execution.")]
[TypeOption(typeof(ExecutionEndpoints), "PauseExecution")]
public class PauseOperationExecutionOption : ExecutionEndpointBase<PauseOperationExecutionEndpoint>
{
}
