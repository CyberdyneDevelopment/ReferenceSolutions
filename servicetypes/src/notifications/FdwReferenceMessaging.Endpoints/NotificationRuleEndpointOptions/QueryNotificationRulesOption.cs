using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>The QueryNotificationRules endpoint.</summary>
[WebMcpTool("query_notification_rules", "Query notification rules by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(NotificationRuleEndpoints), "QueryNotificationRules")]
public class QueryNotificationRulesOption : NotificationRuleEndpointBase<QueryNotificationRulesEndpoint>
{
}
