using Fdw.Collections.Attributes;

namespace ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

/// <summary>The InspectEdge endpoint.</summary>
[TypeOption(typeof(EtlExecutionsEndpoints), "InspectEdge")]
public class InspectEdgeOption : EtlExecutionsEndpointBase<InspectEdgeEndpoint>
{
}
