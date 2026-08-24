using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.Email;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.Email;

/// <summary>
/// Factory interface for creating email notification service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// </remarks>
public interface IEmailNotificationFactory : INotificationFactory<INotificationService, EmailNotificationConfiguration>
{
    // Inherits CreateNotification methods from base interface
}
