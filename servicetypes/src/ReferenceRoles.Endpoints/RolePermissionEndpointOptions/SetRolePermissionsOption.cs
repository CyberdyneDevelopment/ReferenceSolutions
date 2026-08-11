using Fdw.Collections.Attributes;

namespace ReferenceRoles.Endpoints.RolePermissionEndpointOptions;

/// <summary>The SetRolePermissions endpoint.</summary>
[TypeOption(typeof(RolePermissionEndpoints), "SetRolePermissions")]
public class SetRolePermissionsOption : RolePermissionEndpointBase<SetRolePermissionsEndpoint>
{
}
