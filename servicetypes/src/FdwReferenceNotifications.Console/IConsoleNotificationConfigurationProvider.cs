using Fdw.Services.Abstractions;

using Fdw.Services.Notifications.Abstractions;
namespace FdwReferenceNotifications.Console;

/// <summary>Supplies the ConsoleNotification implementation's own configuration.</summary>
public interface IConsoleNotificationConfigurationProvider
    : IImplementationConfigurationProvider<INotificationImplementationConfiguration>
{
}
