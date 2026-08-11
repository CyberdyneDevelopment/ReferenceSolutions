using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The ApproveExecution endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "ApproveExecution")]
public class ApproveExecutionOption : EtlExecutionsEndpointBase<ApproveExecutionEndpoint>
{
}
