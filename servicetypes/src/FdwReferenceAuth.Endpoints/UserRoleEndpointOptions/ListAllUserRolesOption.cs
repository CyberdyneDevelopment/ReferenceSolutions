using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserRoleEndpointOptions;

/// <summary>The ListAllUserRoles endpoint.</summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and a users list asking for roles gets a 404 — which reads as "this
/// host has no such route" rather than "this declaration is missing".
/// </remarks>
[WebMcpTool("list_all_user_roles", "List every user's role assignments", ReadOnly = true)]
[TypeOption(typeof(UserRoleEndpoints), "ListAllUserRoles")]
public class ListAllUserRolesOption : UserRoleEndpointBase<ListAllUserRolesEndpoint>
{
}
