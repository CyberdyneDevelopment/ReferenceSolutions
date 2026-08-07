using Fdw.Services.Notifications.Abstractions;
using ReferenceNotifications.System;
using Fdw.Services.Notifications.System.Commands;
using Fdw.Services.Notifications.System;
using Fdw.Services.Notifications;
using Fdw.Services;
using Fdw;

namespace ReferenceNotifications.System;

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
