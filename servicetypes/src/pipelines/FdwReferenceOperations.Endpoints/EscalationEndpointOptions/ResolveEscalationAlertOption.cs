using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The ResolveEscalationAlert endpoint.</summary>
[WebMcpTool("resolve_escalation_alert", "Resolve an escalation alert.")]
[TypeOption(typeof(EscalationEndpoints), "ResolveEscalationAlert")]
public class ResolveEscalationAlertOption : EscalationEndpointBase<ResolveEscalationAlertEndpoint>
{
}
