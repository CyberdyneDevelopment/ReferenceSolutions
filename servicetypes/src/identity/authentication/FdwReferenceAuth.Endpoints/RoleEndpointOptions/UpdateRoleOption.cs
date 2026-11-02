using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RoleEndpointOptions;

/// <summary>The UpdateRole endpoint.</summary>
[WebMcpTool("update_role", "Update a role")]
[TypeOption(typeof(RoleEndpoints), "UpdateRole")]
public class UpdateRoleOption : RoleEndpointBase<UpdateRoleEndpoint>
{
}
