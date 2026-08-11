using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The ResumeExecution endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "ResumeExecution")]
public class ResumeExecutionOption : EtlExecutionsEndpointBase<ResumeExecutionEndpoint>
{
}
