using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Email.Registration;

/// <summary>The EmailNotification implementation of its domain.</summary>
public interface IEmailNotificationProvider : IImplementationServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>
{
}
