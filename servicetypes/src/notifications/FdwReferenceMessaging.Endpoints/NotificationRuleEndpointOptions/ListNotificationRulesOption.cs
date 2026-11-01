using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>The ListNotificationRules endpoint.</summary>
[WebMcpTool("list_notification_rules", "List notification rules.", ReadOnly = true)]
[TypeOption(typeof(NotificationRuleEndpoints), "ListNotificationRules")]
public class ListNotificationRulesOption : NotificationRuleEndpointBase<ListNotificationRulesEndpoint>
{
}
