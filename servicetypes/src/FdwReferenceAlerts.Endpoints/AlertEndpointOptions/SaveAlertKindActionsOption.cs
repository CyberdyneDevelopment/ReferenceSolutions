using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The SaveAlertKindActions endpoint.</summary>
[WebMcpTool("save_alert_kind_actions", "Choose the actions a kind of alert offers the company.")]
[TypeOption(typeof(AlertEndpoints), "SaveAlertKindActions")]
public class SaveAlertKindActionsOption : AlertEndpointBase<SaveAlertKindActionsEndpoint>
{
}
