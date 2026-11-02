using Fdw.Services.Abstractions;

using Fdw.Services.Notifications.Abstractions;
namespace FdwReferenceNotifications.Email;

/// <summary>Supplies the EmailNotification implementation's own configuration.</summary>
public interface IEmailNotificationConfigurationProvider
    : IImplementationConfigurationProvider<INotificationImplementationConfiguration>
{
}
