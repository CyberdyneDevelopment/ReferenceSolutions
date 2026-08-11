using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The StepExecution endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "StepExecution")]
public class StepExecutionOption : EtlExecutionsEndpointBase<StepExecutionEndpoint>
{
}
