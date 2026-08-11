using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list user notification preferences.
/// Loads persisted preferences from notify.UserNotificationPreference;
/// the base supplies defaults when none are stored.
/// </summary>
public class ListUserPreferencesEndpoint : ListUserPreferencesEndpointBase
{
    private readonly IUserNotificationPreferenceService _preferenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListUserPreferencesEndpoint"/> class.
    /// </summary>
    public ListUserPreferencesEndpoint(IUserNotificationPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<UserNotificationPreferenceDto>> LoadPreferences(Guid userId, CancellationToken ct)
    {
        var result = await _preferenceService.GetPreferences(userId, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.CurrentMessage ?? "Failed to load notification preferences.");
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
