using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationListEndpointOptions;

/// <summary>The ListNotificationLists endpoint.</summary>
[WebMcpTool("list_notification_lists", "List notification lists.", ReadOnly = true)]
[TypeOption(typeof(NotificationListEndpoints), "ListNotificationLists")]
public class ListNotificationListsOption : NotificationListEndpointBase<ListNotificationListsEndpoint>
{
}
