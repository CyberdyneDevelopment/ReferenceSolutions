using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Webhook.Registration;

/// <summary>The WebhookNotification implementation of its domain.</summary>
public interface IWebhookNotificationProvider : IImplementationServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>
{
}
