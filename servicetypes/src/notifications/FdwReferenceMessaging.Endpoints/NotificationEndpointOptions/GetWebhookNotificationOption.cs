using Fdw.Collections.Attributes;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>Serves the get Webhook notification service endpoint (FDW-1909).</summary>
[TypeOption(typeof(NotificationEndpoints), "GetWebhookNotification")]
public class GetWebhookNotificationOption : NotificationEndpointBase<GetWebhookNotificationEndpoint>
{
}
