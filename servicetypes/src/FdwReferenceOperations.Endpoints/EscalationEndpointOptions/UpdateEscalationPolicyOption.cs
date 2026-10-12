using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The UpdateEscalationPolicy endpoint.</summary>
[WebMcpTool("update_escalation_policy", "Update escalation policy.")]
[TypeOption(typeof(EscalationEndpoints), "UpdateEscalationPolicy")]
public class UpdateEscalationPolicyOption : EscalationEndpointBase<UpdateEscalationPolicyEndpoint>
{
}
