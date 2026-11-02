using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Console.Registration;

/// <summary>The ConsoleNotification implementation of its domain.</summary>
public interface IConsoleNotificationProvider : IImplementationServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>
{
}
