using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Webhook;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Webhook;

/// <summary>
/// Factory interface for creating webhook notification service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// </remarks>
public interface IWebhookNotificationFactory : INotificationFactory<INotificationService, WebhookNotificationConfiguration>
{
    // Inherits CreateNotification methods from base interface
}
