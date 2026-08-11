using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationListEndpointOptions;

/// <summary>The ListNotificationLists endpoint.</summary>
[TypeOption(typeof(NotificationListEndpoints), "ListNotificationLists")]
public class ListNotificationListsOption : NotificationListEndpointBase<ListNotificationListsEndpoint>
{
}
