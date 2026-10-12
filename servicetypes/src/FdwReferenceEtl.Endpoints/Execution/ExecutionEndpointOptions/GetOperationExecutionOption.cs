using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The GetExecution endpoint.</summary>
[WebMcpTool("get_execution", "Get execution.", ReadOnly = true)]
[TypeOption(typeof(ExecutionEndpoints), "GetExecution")]
public class GetOperationExecutionOption : ExecutionEndpointBase<GetOperationExecutionEndpoint>
{
}
