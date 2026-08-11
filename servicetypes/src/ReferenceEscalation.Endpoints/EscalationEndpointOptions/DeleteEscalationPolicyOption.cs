using Fdw.Collections.Attributes;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The DeleteEscalationPolicy endpoint.</summary>
[TypeOption(typeof(EscalationEndpoints), "DeleteEscalationPolicy")]
public class DeleteEscalationPolicyOption : EscalationEndpointBase<DeleteEscalationPolicyEndpoint>
{
}
