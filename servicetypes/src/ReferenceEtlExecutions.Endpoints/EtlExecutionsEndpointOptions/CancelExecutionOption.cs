using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The CancelExecution endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "CancelExecution")]
public class CancelExecutionOption : EtlExecutionsEndpointBase<CancelExecutionEndpoint>
{
}
