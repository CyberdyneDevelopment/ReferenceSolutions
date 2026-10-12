using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.PermissionEndpointOptions;

/// <summary>The ListPermissions endpoint.</summary>
[WebMcpTool("list_permissions", "List all permissions", ReadOnly = true)]
[TypeOption(typeof(PermissionEndpoints), "ListPermissions")]
public class ListPermissionsOption : PermissionEndpointBase<ListPermissionsEndpoint>
{
}
