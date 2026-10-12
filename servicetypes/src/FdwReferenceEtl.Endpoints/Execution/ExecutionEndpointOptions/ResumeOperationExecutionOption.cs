using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The ResumeExecution endpoint.</summary>
[WebMcpTool("resume_execution", "Resume execution.")]
[TypeOption(typeof(ExecutionEndpoints), "ResumeExecution")]
public class ResumeOperationExecutionOption : ExecutionEndpointBase<ResumeOperationExecutionEndpoint>
{
}
