using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint: the notification types and the settings each needs (FDW-2931).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationTypesEndpoint : ListNotificationTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListNotificationTypesEndpoint"/> class.</summary>
    public ListNotificationTypesEndpoint(ILogger<ListNotificationTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
