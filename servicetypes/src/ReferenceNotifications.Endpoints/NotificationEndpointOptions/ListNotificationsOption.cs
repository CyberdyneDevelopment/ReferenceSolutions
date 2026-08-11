using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The ListNotifications endpoint.</summary>
[TypeOption(typeof(NotificationEndpoints), "ListNotifications")]
public class ListNotificationsOption : NotificationEndpointBase<ListNotificationsEndpoint>
{
}
