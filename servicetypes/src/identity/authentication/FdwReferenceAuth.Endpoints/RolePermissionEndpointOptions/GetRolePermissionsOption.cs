using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RolePermissionEndpointOptions;

/// <summary>The GetRolePermissions endpoint.</summary>
[WebMcpTool("get_role_permissions", "Get role permissions", ReadOnly = true)]
[TypeOption(typeof(RolePermissionEndpoints), "GetRolePermissions")]
public class GetRolePermissionsOption : RolePermissionEndpointBase<GetRolePermissionsEndpoint>
{
}
