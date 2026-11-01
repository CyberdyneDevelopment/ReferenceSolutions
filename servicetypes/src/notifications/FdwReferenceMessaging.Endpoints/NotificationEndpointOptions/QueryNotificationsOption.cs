using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>The QueryNotifications endpoint.</summary>
[WebMcpTool("query_notifications", "Query notifications by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(NotificationEndpoints), "QueryNotifications")]
public class QueryNotificationsOption : NotificationEndpointBase<QueryNotificationsEndpoint>
{
}
