using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The GetExecutionEvents endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "GetExecutionEvents")]
public class GetExecutionEventsOption : ExecutionEndpointBase<GetExecutionEventsEndpoint>
{
}
