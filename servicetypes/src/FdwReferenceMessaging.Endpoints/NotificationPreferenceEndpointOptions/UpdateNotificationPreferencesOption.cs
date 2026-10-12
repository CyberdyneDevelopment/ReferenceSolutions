using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

/// <summary>The UpdateUserPreferences endpoint.</summary>
[WebMcpTool("messaging_update_user_preferences", "Update user preferences.")]
[TypeOption(typeof(NotificationPreferenceEndpoints), "UpdateUserPreferences")]
public class UpdateNotificationPreferencesOption : NotificationPreferenceEndpointBase<UpdateUserPreferencesEndpoint>
{
}
