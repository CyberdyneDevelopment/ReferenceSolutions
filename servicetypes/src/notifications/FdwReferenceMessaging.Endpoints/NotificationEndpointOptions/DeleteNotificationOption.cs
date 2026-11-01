using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

[WebMcpTool("delete_notification", "Delete notification.")]
[TypeOption(typeof(NotificationEndpoints), "DeleteNotification")]
public class DeleteNotificationOption : NotificationEndpointBase<DeleteNotificationEndpoint>
{
}
