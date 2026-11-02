using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The ListAlerts endpoint.</summary>
[WebMcpTool("list_alerts", "Say what alerts allow the caller.")]
[TypeOption(typeof(AlertEndpoints), "ListAlerts")]
public class ListAlertsOption : AlertEndpointBase<ListAlertsEndpoint>
{
}
