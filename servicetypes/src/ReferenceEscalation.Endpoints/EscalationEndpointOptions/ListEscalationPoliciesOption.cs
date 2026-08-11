using Fdw.Collections.Attributes;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The ListEscalationPolicies endpoint.</summary>
[TypeOption(typeof(EscalationEndpoints), "ListEscalationPolicies")]
public class ListEscalationPoliciesOption : EscalationEndpointBase<ListEscalationPoliciesEndpoint>
{
}
