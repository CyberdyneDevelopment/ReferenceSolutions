using Fdw.Collections.Attributes;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The GetEscalationPolicy endpoint.</summary>
[TypeOption(typeof(EscalationEndpoints), "GetEscalationPolicy")]
public class GetEscalationPolicyOption : EscalationEndpointBase<GetEscalationPolicyEndpoint>
{
}
