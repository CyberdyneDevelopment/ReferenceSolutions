using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The GetExecution endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "GetExecution")]
public class GetExecutionOption : ExecutionEndpointBase<GetExecutionEndpoint>
{
}
