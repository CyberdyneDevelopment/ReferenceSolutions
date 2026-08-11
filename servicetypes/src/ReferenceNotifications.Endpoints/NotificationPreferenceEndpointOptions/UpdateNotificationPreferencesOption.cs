using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

/// <summary>The UpdateUserPreferences endpoint.</summary>
[TypeOption(typeof(NotificationPreferenceEndpoints), "UpdateUserPreferences")]
public class UpdateNotificationPreferencesOption : NotificationPreferenceEndpointBase<UpdateUserPreferencesEndpoint>
{
}
