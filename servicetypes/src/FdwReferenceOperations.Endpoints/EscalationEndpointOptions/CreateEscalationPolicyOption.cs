using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The CreateEscalationPolicy endpoint.</summary>
[WebMcpTool("create_escalation_policy", "Create escalation policy.")]
[TypeOption(typeof(EscalationEndpoints), "CreateEscalationPolicy")]
public class CreateEscalationPolicyOption : EscalationEndpointBase<CreateEscalationPolicyEndpoint>
{
}
