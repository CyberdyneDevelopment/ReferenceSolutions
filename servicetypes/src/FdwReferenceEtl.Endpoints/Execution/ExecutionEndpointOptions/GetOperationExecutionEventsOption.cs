using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The GetExecutionEvents endpoint.</summary>
[WebMcpTool("get_execution_events", "Get execution events.", ReadOnly = true)]
[TypeOption(typeof(ExecutionEndpoints), "GetExecutionEvents")]
public class GetOperationExecutionEventsOption : ExecutionEndpointBase<GetOperationExecutionEventsEndpoint>
{
}
