using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The ResumeExecution endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "ResumeExecution")]
public class ResumeExecutionOption : ExecutionEndpointBase<ResumeExecutionEndpoint>
{
}
