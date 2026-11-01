using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The GetEscalationPolicy endpoint.</summary>
[WebMcpTool("get_escalation_policy", "Get escalation policy.", ReadOnly = true)]
[TypeOption(typeof(EscalationEndpoints), "GetEscalationPolicy")]
public class GetEscalationPolicyOption : EscalationEndpointBase<GetEscalationPolicyEndpoint>
{
}
