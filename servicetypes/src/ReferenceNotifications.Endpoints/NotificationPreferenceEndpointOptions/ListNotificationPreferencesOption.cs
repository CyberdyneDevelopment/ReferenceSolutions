using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

/// <summary>The ListUserPreferences endpoint.</summary>
[TypeOption(typeof(NotificationPreferenceEndpoints), "ListUserPreferences")]
public class ListNotificationPreferencesOption : NotificationPreferenceEndpointBase<ListUserPreferencesEndpoint>
{
}
