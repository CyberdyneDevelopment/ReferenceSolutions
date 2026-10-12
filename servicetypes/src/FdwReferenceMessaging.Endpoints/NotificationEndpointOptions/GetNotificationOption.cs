using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The GetNotification endpoint.</summary>
[WebMcpTool("get_notification", "Get notification.", ReadOnly = true)]
[TypeOption(typeof(NotificationEndpoints), "GetNotification")]
public class GetNotificationOption : NotificationEndpointBase<GetNotificationEndpoint>
{
}
