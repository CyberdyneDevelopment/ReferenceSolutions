using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceRoles.Endpoints.RolePermissionEndpointOptions;

/// <summary>
/// The endpoints over the role-permission resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "RolePermissionEndpoints")]
[TypeCollection(typeof(RolePermissionEndpointBase), typeof(IEndpointTypeOption), typeof(RolePermissionEndpoints))]
public partial class RolePermissionEndpoints : EndpointTypeCollectionBase<RolePermissionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
