using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The CreateNotification endpoint.</summary>
[WebMcpTool("create_notification", "Create notification.")]
[TypeOption(typeof(NotificationEndpoints), "CreateNotification")]
public class CreateNotificationOption : NotificationEndpointBase<CreateNotificationEndpoint>
{
}
