using Fdw.Collections.Attributes;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>Serves the update Webhook notification service endpoint (FDW-1909).</summary>
[TypeOption(typeof(NotificationEndpoints), "UpdateWebhookNotification")]
public class UpdateWebhookNotificationOption : NotificationEndpointBase<UpdateWebhookNotificationEndpoint>
{
}
