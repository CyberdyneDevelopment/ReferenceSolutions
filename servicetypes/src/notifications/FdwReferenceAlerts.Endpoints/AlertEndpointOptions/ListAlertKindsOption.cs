using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The ListAlertKinds endpoint.</summary>
[WebMcpTool("list_alert_kinds", "Say which actions each kind of alert offers the company.")]
[TypeOption(typeof(AlertEndpoints), "ListAlertKinds")]
public class ListAlertKindsOption : AlertEndpointBase<ListAlertKindsEndpoint>
{
}
