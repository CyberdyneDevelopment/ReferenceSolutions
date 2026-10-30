using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>The ListNotificationRuleSeverities endpoint (FDW-2931).</summary>
[WebMcpTool("list_notification_rule_severities", "List the severities a notification rule may carry.", ReadOnly = true)]
[TypeOption(typeof(NotificationRuleEndpoints), "ListNotificationRuleSeverities")]
public class ListNotificationRuleSeveritiesOption : NotificationRuleEndpointBase<ListNotificationRuleSeveritiesEndpoint>
{
}
