using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The PauseExecution endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "PauseExecution")]
public class PauseExecutionOption : ExecutionEndpointBase<PauseExecutionEndpoint>
{
}
