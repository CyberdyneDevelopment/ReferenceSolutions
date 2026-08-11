using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The CreateNotification endpoint.</summary>
[TypeOption(typeof(NotificationEndpoints), "CreateNotification")]
public class CreateNotificationOption : NotificationEndpointBase<CreateNotificationEndpoint>
{
}
