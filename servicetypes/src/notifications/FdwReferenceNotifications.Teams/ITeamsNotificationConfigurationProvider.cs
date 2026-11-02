using Fdw.Services.Abstractions;

using Fdw.Services.Notifications.Abstractions;
namespace FdwReferenceNotifications.Teams;

/// <summary>Supplies the TeamsNotification implementation's own configuration.</summary>
public interface ITeamsNotificationConfigurationProvider
    : IImplementationConfigurationProvider<INotificationImplementationConfiguration>
{
}
