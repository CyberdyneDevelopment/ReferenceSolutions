using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The GetExecutionStatus endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "GetExecutionStatus")]
public class GetExecutionStatusOption : EtlExecutionsEndpointBase<GetExecutionStatusEndpoint>
{
}
