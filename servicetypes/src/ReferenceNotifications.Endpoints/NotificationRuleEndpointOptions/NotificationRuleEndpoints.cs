using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>
/// The endpoints over the notification-rule resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "NotificationRuleEndpoints")]
[TypeCollection(typeof(NotificationRuleEndpointBase), typeof(IEndpointTypeOption), typeof(NotificationRuleEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "NotificationRuleEndpoints")]
public partial class NotificationRuleEndpoints : EndpointTypeCollectionBase<NotificationRuleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
