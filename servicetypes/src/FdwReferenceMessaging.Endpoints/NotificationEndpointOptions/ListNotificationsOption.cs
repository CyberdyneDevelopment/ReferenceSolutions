using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The ListNotifications endpoint.</summary>
[WebMcpTool("list_notifications", "List notifications.", ReadOnly = true)]
[TypeOption(typeof(NotificationEndpoints), "ListNotifications")]
public class ListNotificationsOption : NotificationEndpointBase<ListNotificationsEndpoint>
{
}
