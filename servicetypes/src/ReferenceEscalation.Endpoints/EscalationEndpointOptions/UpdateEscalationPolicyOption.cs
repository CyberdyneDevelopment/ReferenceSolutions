using Fdw.Collections.Attributes;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The UpdateEscalationPolicy endpoint.</summary>
[TypeOption(typeof(EscalationEndpoints), "UpdateEscalationPolicy")]
public class UpdateEscalationPolicyOption : EscalationEndpointBase<UpdateEscalationPolicyEndpoint>
{
}
