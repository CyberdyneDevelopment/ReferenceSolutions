using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to update user notification preferences.
/// Persists the toggled preferences to notify.UserNotificationPreference and returns
/// the persisted state read back from storage.
/// </summary>
public class UpdateUserPreferencesEndpoint : UpdateUserPreferencesEndpointBase
{
    private readonly IUserNotificationPreferenceService _preferenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateUserPreferencesEndpoint"/> class.
    /// </summary>
    public UpdateUserPreferencesEndpoint(IUserNotificationPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<UserNotificationPreferenceDto>> SavePreferences(
        Guid userId,
        IReadOnlyList<UserNotificationPreferenceDto> preferences,
        CancellationToken ct)
    {
        var toSave = preferences
            .Select(p => new NotificationPreference
            {
                NotificationType = p.NotificationType,
                Channel = p.Channel,
                IsEnabled = p.IsEnabled,
            })
            .ToList();

        var result = await _preferenceService.SavePreferences(userId, toSave, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.CurrentMessage ?? "Failed to persist notification preferences.");
        }

        return (result.Value ?? [])
            .Select(p => new UserNotificationPreferenceDto
            {
                NotificationType = p.NotificationType,
                Channel = p.Channel,
                IsEnabled = p.IsEnabled,
            })
            .ToList();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Notifications");
    }
}
