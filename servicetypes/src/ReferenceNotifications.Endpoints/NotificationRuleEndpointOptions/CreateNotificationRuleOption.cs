using Fdw.Collections.Attributes;

namespace ReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>The CreateNotificationRule endpoint.</summary>
[TypeOption(typeof(NotificationRuleEndpoints), "CreateNotificationRule")]
public class CreateNotificationRuleOption : NotificationRuleEndpointBase<CreateNotificationRuleEndpoint>
{
}
