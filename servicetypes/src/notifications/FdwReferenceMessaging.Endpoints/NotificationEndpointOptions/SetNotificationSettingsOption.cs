using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The SetNotificationSettings endpoint (FDW-2931).</summary>
[WebMcpTool("set_notification_settings", "Set the settings of a notification.")]
[TypeOption(typeof(NotificationEndpoints), "SetNotificationSettings")]
public class SetNotificationSettingsOption : NotificationEndpointBase<SetNotificationSettingsEndpoint>
{
}
