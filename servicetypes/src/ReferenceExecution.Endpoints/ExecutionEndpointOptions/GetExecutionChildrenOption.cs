using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The GetExecutionChildren endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "GetExecutionChildren")]
public class GetExecutionChildrenOption : ExecutionEndpointBase<GetExecutionChildrenEndpoint>
{
}
