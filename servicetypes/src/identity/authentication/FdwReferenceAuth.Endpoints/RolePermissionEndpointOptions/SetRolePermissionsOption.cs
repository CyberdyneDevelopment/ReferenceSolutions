using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RolePermissionEndpointOptions;

/// <summary>The SetRolePermissions endpoint.</summary>
[WebMcpTool("set_role_permissions", "Set role permissions")]
[TypeOption(typeof(RolePermissionEndpoints), "SetRolePermissions")]
public class SetRolePermissionsOption : RolePermissionEndpointBase<SetRolePermissionsEndpoint>
{
}
