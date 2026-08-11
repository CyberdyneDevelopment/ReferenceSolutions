using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Endpoints;
using Microsoft.Extensions.Options;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to get a notification configuration by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetNotificationEndpoint : GetNotificationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationEndpoint"/> class.
    /// </summary>
    public GetNotificationEndpoint(NotificationConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
