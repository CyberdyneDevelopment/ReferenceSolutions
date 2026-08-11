using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The InspectTask endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "InspectTask")]
public class InspectTaskOption : EtlExecutionsEndpointBase<InspectTaskEndpoint>
{
}
