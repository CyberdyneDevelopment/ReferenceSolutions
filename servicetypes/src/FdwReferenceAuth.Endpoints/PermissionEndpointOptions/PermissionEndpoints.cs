using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceAuth.Endpoints.PermissionEndpointOptions;

/// <summary>
/// The endpoints over the permission resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "PermissionEndpoints")]
[TypeCollection(typeof(PermissionEndpointBase), typeof(IEndpointTypeOption), typeof(PermissionEndpoints))]
public partial class PermissionEndpoints : EndpointTypeCollectionBase<PermissionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
