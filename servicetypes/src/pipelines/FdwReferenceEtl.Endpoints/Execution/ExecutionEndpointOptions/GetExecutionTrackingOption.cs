using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The GetExecutionTracking endpoint.</summary>
[WebMcpTool("get_execution_tracking", "Get what a run did: rows, size and run time of the run, each stage and each component.", ReadOnly = true)]
[TypeOption(typeof(ExecutionEndpoints), "GetExecutionTracking")]
public class GetExecutionTrackingOption : ExecutionEndpointBase<GetExecutionTrackingEndpoint>
{
}
