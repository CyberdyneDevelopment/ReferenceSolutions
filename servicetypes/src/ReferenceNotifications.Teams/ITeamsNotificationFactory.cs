using Fdw.Services.Notifications.Abstractions;

namespace ReferenceNotifications.Teams;

/// <summary>Creates the Teams notification service for a configured Teams target.</summary>
public interface ITeamsNotificationFactory : INotificationFactory<INotificationService, TeamsNotificationConfiguration>
{
    // Inherits CreateNotification methods from base interface
}
