using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to query notification configurations by filter, order, page and field: <c>POST /notifications/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API exposes
/// of a notification is this application's decision. They are the fields the notifications list shows, each with
/// the column it reaches and the type its values have. Deliberately absent: the row's tenancy and visibility
/// columns, its <c>domain</c> name, and everything that lives on the notification's implementation -- none of it
/// can be filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryNotificationsEndpoint : QueryNotificationsEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("implementation", "Implementation", typeof(string)),
    ];

    /// <inheritdoc />
    public QueryNotificationsEndpoint(NotificationConfigurationProvider provider, ILogger<QueryNotificationsEndpoint> logger)
        : base(logger ?? NullLogger<QueryNotificationsEndpoint>.Instance, provider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
