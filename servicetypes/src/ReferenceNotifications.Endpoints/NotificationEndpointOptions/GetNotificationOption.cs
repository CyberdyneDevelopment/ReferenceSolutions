using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The GetNotification endpoint.</summary>
[TypeOption(typeof(NotificationEndpoints), "GetNotification")]
public class GetNotificationOption : NotificationEndpointBase<GetNotificationEndpoint>
{
}
