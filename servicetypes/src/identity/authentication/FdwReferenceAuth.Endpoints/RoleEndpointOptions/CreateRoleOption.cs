using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RoleEndpointOptions;

/// <summary>The CreateRole endpoint.</summary>
[WebMcpTool("create_role", "Create a new role")]
[TypeOption(typeof(RoleEndpoints), "CreateRole")]
public class CreateRoleOption : RoleEndpointBase<CreateRoleEndpoint>
{
}
