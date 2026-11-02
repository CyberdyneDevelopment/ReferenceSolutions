using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Console;

/// <summary>
/// Factory interface for creating console notification service instances.
/// </summary>
/// <remarks>
/// Registered as singleton in DI (Phase 1) and resolved in Phase 2.
/// </remarks>
public interface IConsoleNotificationFactory : INotificationFactory<INotificationService, ConsoleNotificationConfiguration>
{
    // Inherits CreateNotification methods from base interface
}
