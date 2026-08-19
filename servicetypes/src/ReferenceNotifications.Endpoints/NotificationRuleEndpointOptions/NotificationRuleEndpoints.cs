using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceNotifications.Endpoints.NotificationRuleEndpointOptions;

/// <summary>
/// The endpoints over the notification-rule resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "NotificationRuleEndpoints")]
[TypeCollection(typeof(NotificationRuleEndpointBase), typeof(IEndpointTypeOption), typeof(NotificationRuleEndpoints))]
public partial class NotificationRuleEndpoints : EndpointTypeCollectionBase<NotificationRuleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
