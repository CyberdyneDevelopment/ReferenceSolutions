using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Endpoints;

namespace ReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to list all notification lists (recipient groups).
/// Returns an empty list until database-backed lists are implemented.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListNotificationListsEndpoint : ListNotificationListsEndpointBase
{
    /// <inheritdoc />
    protected override Task<IGenericResult<List<NotificationListSummaryDto>>> LoadNotificationLists(CancellationToken ct)
    {
        // TODO: Load from cfg.NotificationList via IOptionsMonitor when ManagedConfiguration is wired
        return Task.FromResult(GenericResult<List<NotificationListSummaryDto>>.Success([]));
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Notifications");
    }
}
