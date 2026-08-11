using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The CancelExecution endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "CancelExecution")]
public class CancelExecutionOption : ExecutionEndpointBase<CancelExecutionEndpoint>
{
}
