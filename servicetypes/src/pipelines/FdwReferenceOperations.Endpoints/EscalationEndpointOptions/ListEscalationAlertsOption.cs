using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The ListEscalationAlerts endpoint.</summary>
[WebMcpTool("list_escalation_alerts", "List escalation alerts.", ReadOnly = true)]
[TypeOption(typeof(EscalationEndpoints), "ListEscalationAlerts")]
public class ListEscalationAlertsOption : EscalationEndpointBase<ListEscalationAlertsEndpoint>
{
}
