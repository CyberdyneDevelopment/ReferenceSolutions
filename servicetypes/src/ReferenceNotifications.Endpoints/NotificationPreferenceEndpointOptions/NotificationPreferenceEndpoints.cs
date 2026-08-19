using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

/// <summary>
/// The endpoints over the user-preference resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "NotificationPreferenceEndpoints")]
[TypeCollection(typeof(NotificationPreferenceEndpointBase), typeof(IEndpointTypeOption), typeof(NotificationPreferenceEndpoints))]
public partial class NotificationPreferenceEndpoints : EndpointTypeCollectionBase<NotificationPreferenceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
