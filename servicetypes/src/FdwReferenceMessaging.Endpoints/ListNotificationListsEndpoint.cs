using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list all notification lists (recipient groups), as the configuration store holds them.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationListsEndpoint : ListNotificationListsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListNotificationListsEndpoint"/> class.</summary>
    public ListNotificationListsEndpoint(ILogger<ListNotificationListsEndpoint> logger, INotificationLists lists) : base(logger, lists)
    {
    }

    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
