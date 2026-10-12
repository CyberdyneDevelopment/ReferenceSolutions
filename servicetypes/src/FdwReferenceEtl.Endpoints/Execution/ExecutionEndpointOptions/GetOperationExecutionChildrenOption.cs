using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The GetExecutionChildren endpoint.</summary>
[WebMcpTool("get_execution_children", "Get execution children.", ReadOnly = true)]
[TypeOption(typeof(ExecutionEndpoints), "GetExecutionChildren")]
public class GetOperationExecutionChildrenOption : ExecutionEndpointBase<GetOperationExecutionChildrenEndpoint>
{
}
