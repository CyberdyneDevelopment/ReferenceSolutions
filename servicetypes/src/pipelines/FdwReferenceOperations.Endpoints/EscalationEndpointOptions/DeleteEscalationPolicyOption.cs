using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The DeleteEscalationPolicy endpoint.</summary>
[WebMcpTool("delete_escalation_policy", "Delete escalation policy.")]
[TypeOption(typeof(EscalationEndpoints), "DeleteEscalationPolicy")]
public class DeleteEscalationPolicyOption : EscalationEndpointBase<DeleteEscalationPolicyEndpoint>
{
}
