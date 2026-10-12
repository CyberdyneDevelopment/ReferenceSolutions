using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.System.Registration;

/// <summary>The SystemNotification implementation of its domain.</summary>
public interface ISystemNotificationProvider : IImplementationServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>
{
}
