using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RoleEndpointOptions;

/// <summary>The ListRoles endpoint.</summary>
[WebMcpTool("list_roles", "List all roles", ReadOnly = true)]
[TypeOption(typeof(RoleEndpoints), "ListRoles")]
public class ListRolesOption : RoleEndpointBase<ListRolesEndpoint>
{
}
