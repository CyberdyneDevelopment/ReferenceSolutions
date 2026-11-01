using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>The CreateNotificationRule endpoint.</summary>
[WebMcpTool("create_notification_rule", "Create notification rule.")]
[TypeOption(typeof(NotificationRuleEndpoints), "CreateNotificationRule")]
public class CreateNotificationRuleOption : NotificationRuleEndpointBase<CreateNotificationRuleEndpoint>
{
}
