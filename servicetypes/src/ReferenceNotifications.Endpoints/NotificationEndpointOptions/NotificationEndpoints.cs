using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceNotifications.Endpoints.NotificationEndpointOptions;

/// <summary>
/// The endpoints over the notification resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "NotificationEndpoints")]
[TypeCollection(typeof(NotificationEndpointBase), typeof(IEndpointTypeOption), typeof(NotificationEndpoints))]
public partial class NotificationEndpoints : EndpointTypeCollectionBase<NotificationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
