using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The PauseExecution endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "PauseExecution")]
public class PauseExecutionOption : EtlExecutionsEndpointBase<PauseExecutionEndpoint>
{
}
