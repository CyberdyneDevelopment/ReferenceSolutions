using Fdw.Services.Abstractions;
using Fdw.Services.Notifications.Abstractions;

namespace FdwReferenceNotifications.Teams.Registration;

/// <summary>The TeamsNotification implementation of its domain.</summary>
public interface ITeamsNotificationProvider : IImplementationServiceProvider<IPlatformNotification, INotificationImplementationConfiguration>
{
}
