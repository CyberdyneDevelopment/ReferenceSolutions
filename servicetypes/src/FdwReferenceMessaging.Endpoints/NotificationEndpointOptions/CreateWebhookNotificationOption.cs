using Fdw.Collections.Attributes;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>Serves the create Webhook notification service endpoint (FDW-1909).</summary>
[TypeOption(typeof(NotificationEndpoints), "CreateWebhookNotification")]
public class CreateWebhookNotificationOption : NotificationEndpointBase<CreateWebhookNotificationEndpoint>
{
}
