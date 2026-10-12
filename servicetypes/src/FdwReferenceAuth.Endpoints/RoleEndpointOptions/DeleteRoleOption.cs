using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RoleEndpointOptions;

/// <summary>The DeleteRole endpoint.</summary>
[WebMcpTool("delete_role", "Delete a role")]
[TypeOption(typeof(RoleEndpoints), "DeleteRole")]
public class DeleteRoleOption : RoleEndpointBase<DeleteRoleEndpoint>
{
}
