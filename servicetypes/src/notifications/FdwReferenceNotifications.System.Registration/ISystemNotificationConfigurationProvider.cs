using Fdw.Services.Abstractions;

using Fdw.Services.Notifications.Abstractions;
namespace FdwReferenceNotifications.System.Registration;

/// <summary>Supplies the SystemNotification implementation's own configuration.</summary>
public interface ISystemNotificationConfigurationProvider
    : IImplementationConfigurationProvider<INotificationImplementationConfiguration>
{
}
