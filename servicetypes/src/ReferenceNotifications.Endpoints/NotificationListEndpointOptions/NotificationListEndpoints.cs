using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceNotifications.Endpoints.NotificationListEndpointOptions;

/// <summary>
/// The endpoints over the notification-list resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "NotificationListEndpoints")]
[TypeCollection(typeof(NotificationListEndpointBase), typeof(IEndpointTypeOption), typeof(NotificationListEndpoints))]
public partial class NotificationListEndpoints : EndpointTypeCollectionBase<NotificationListEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
