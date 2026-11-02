using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.System;

namespace FdwReferenceNotifications.System;

/// <summary>
/// Factory interface for creating system notification service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// </remarks>
public interface ISystemNotificationFactory : INotificationFactory<INotificationService, SystemNotificationConfiguration>
{
    // Inherits CreateNotification methods from base interface
}
