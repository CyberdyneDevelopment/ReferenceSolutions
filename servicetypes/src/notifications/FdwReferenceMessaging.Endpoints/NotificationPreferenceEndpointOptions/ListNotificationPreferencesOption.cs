using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

/// <summary>The ListUserPreferences endpoint.</summary>
[WebMcpTool("list_user_preferences", "List user preferences.", ReadOnly = true)]
[TypeOption(typeof(NotificationPreferenceEndpoints), "ListUserPreferences")]
public class ListNotificationPreferencesOption : NotificationPreferenceEndpointBase<ListUserPreferencesEndpoint>
{
}
