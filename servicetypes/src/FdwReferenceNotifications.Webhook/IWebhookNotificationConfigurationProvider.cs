using Fdw.Services.Abstractions;

using Fdw.Services.Notifications.Abstractions;
namespace FdwReferenceNotifications.Webhook;

/// <summary>Supplies the WebhookNotification implementation's own configuration.</summary>
public interface IWebhookNotificationConfigurationProvider
    : IImplementationConfigurationProvider<INotificationImplementationConfiguration>
{
}
