using Fdw.Collections.Attributes;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The CreateEscalationPolicy endpoint.</summary>
[TypeOption(typeof(EscalationEndpoints), "CreateEscalationPolicy")]
public class CreateEscalationPolicyOption : EscalationEndpointBase<CreateEscalationPolicyEndpoint>
{
}
