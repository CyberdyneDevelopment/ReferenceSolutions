using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The CancelExecution endpoint.</summary>
[WebMcpTool("cancel_execution", "Cancel execution.")]
[TypeOption(typeof(ExecutionEndpoints), "CancelExecution")]
public class CancelOperationExecutionOption : ExecutionEndpointBase<CancelOperationExecutionEndpoint>
{
}
