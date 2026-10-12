using Fdw.Services.Notifications.Abstractions;
﻿using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Endpoints;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list notification configurations, one page at a time.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationsEndpoint : ListNotificationsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListNotificationsEndpoint"/> class.
    /// </summary>
    public ListNotificationsEndpoint(NotificationConfigurationProvider provider, ILogger<ListNotificationsEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
