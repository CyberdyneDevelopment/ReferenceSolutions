using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The ResetAlertKindActions endpoint.</summary>
[WebMcpTool("reset_alert_kind_actions", "Go back to the actions a kind of alert offers by default.")]
[TypeOption(typeof(AlertEndpoints), "ResetAlertKindActions")]
public class ResetAlertKindActionsOption : AlertEndpointBase<ResetAlertKindActionsEndpoint>
{
}
