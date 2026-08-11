using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The ResumeTestExecution endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "ResumeTestExecution")]
public class ResumeTestExecutionOption : EtlExecutionsEndpointBase<ResumeTestExecutionEndpoint>
{
}
