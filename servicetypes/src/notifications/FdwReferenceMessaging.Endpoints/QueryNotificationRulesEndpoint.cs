using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>
/// Endpoint to query notification rules by filter, order, page and field: <c>POST /notifications/rules/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API exposes
/// of a rule is this application's decision. They are the fields the rules list shows, each with the column it
/// reaches and the type its values have. Deliberately absent: the row's tenancy and visibility columns, its
/// <c>domain</c> and <c>implementation</c> names, and everything that lives on the rule's implementation (its
/// template, cooldown, conditions and recipients among them) -- none of it can be filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryNotificationRulesEndpoint : QueryNotificationRulesEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("description", "Description", typeof(string)),
        new("isEnabled", "IsEnabled", typeof(bool)),
        new("notificationServiceType", "NotificationServiceType", typeof(string)),
        new("notificationServiceName", "NotificationServiceName", typeof(string)),
        new("severity", "Severity", typeof(string)),
    ];

    /// <inheritdoc />
    public QueryNotificationRulesEndpoint(NotificationRuleConfigurationProvider provider, ILogger<QueryNotificationRulesEndpoint> logger)
        : base(logger ?? NullLogger<QueryNotificationRulesEndpoint>.Instance, provider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
