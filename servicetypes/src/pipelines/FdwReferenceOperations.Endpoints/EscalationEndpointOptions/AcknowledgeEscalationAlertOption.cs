using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The AcknowledgeEscalationAlert endpoint.</summary>
[WebMcpTool("acknowledge_escalation_alert", "Acknowledge an escalation alert.")]
[TypeOption(typeof(EscalationEndpoints), "AcknowledgeEscalationAlert")]
public class AcknowledgeEscalationAlertOption : EscalationEndpointBase<AcknowledgeEscalationAlertEndpoint>
{
}
