using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The ListNotificationTypes endpoint (FDW-2931).</summary>
[WebMcpTool("list_notification_types", "List the notification types and the settings each needs.", ReadOnly = true)]
[TypeOption(typeof(NotificationEndpoints), "ListNotificationTypes")]
public class ListNotificationTypesOption : NotificationEndpointBase<ListNotificationTypesEndpoint>
{
}
