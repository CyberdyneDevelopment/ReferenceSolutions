using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The ListExecutions endpoint.</summary>
[WebMcpTool("list_executions", "List executions.", ReadOnly = true)]
[TypeOption(typeof(ExecutionEndpoints), "ListExecutions")]
public class ListOperationExecutionsOption : ExecutionEndpointBase<ListOperationExecutionsEndpoint>
{
}
