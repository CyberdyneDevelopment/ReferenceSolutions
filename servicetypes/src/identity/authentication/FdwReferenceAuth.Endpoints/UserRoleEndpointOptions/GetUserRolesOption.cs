using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserRoleEndpointOptions;

/// <summary>The GetUserRoles endpoint.</summary>
[WebMcpTool("get_user_roles", "Get roles assigned to a user", ReadOnly = true)]
[TypeOption(typeof(UserRoleEndpoints), "GetUserRoles")]
public class GetUserRolesOption : UserRoleEndpointBase<GetUserRolesEndpoint>
{
}
