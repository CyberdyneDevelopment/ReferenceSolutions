using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserRoleEndpointOptions;

/// <summary>The RevokeUserRole endpoint.</summary>
[WebMcpTool("revoke_user_role", "Revoke a role from a user")]
[TypeOption(typeof(UserRoleEndpoints), "RevokeUserRole")]
public class RevokeUserRoleOption : UserRoleEndpointBase<RevokeUserRoleEndpoint>
{
}
