using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceRoles.Endpoints.RolePermissionEndpointOptions;

/// <summary>
/// The endpoints over the role-permission resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "RolePermissionEndpoints")]
[TypeCollection(typeof(RolePermissionEndpointBase), typeof(IEndpointTypeOption), typeof(RolePermissionEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "RolePermissionEndpoints")]
public partial class RolePermissionEndpoints : EndpointTypeCollectionBase<RolePermissionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
