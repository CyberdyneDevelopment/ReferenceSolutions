using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The TakeBackAlertAction endpoint.</summary>
[WebMcpTool("take_back_alert_action", "Take back an action on an alert.")]
[TypeOption(typeof(AlertEndpoints), "TakeBackAlertAction")]
public class TakeBackAlertActionOption : AlertEndpointBase<TakeBackAlertActionEndpoint>
{
}
