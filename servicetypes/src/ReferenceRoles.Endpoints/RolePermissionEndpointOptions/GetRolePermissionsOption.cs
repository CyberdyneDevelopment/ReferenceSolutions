using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RolePermissionEndpointOptions;

/// <summary>The GetRolePermissions endpoint.</summary>
[TypeOption(typeof(RolePermissionEndpoints), "GetRolePermissions")]
public class GetRolePermissionsOption : RolePermissionEndpointBase<GetRolePermissionsEndpoint>
{
}
