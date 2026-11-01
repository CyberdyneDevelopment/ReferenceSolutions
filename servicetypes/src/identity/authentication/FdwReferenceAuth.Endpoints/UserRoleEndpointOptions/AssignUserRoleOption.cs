using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserRoleEndpointOptions;

/// <summary>The AssignUserRole endpoint.</summary>
[WebMcpTool("assign_user_role", "Assign a role to a user")]
[TypeOption(typeof(UserRoleEndpoints), "AssignUserRole")]
public class AssignUserRoleOption : UserRoleEndpointBase<AssignUserRoleEndpoint>
{
}
