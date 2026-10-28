using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The ApplyAlertAction endpoint.</summary>
[WebMcpTool("apply_alert_action", "Take an action on an alert.")]
[TypeOption(typeof(AlertEndpoints), "ApplyAlertAction")]
public class ApplyAlertActionOption : AlertEndpointBase<ApplyAlertActionEndpoint>
{
}
