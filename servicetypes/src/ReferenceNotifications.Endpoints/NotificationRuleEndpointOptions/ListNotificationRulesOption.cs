using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>The ListNotificationRules endpoint.</summary>
[TypeOption(typeof(NotificationRuleEndpoints), "ListNotificationRules")]
public class ListNotificationRulesOption : NotificationRuleEndpointBase<ListNotificationRulesEndpoint>
{
}
