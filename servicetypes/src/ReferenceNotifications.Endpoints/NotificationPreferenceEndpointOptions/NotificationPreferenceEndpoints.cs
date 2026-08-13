using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNotifications.Endpoints.NotificationPreferenceEndpointOptions;

/// <summary>
/// The endpoints over the user-preference resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NotificationPreferenceEndpointBase), typeof(IEndpointTypeOption), typeof(NotificationPreferenceEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "NotificationPreferenceEndpoints")]
public partial class NotificationPreferenceEndpoints : EndpointTypeCollectionBase<NotificationPreferenceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
