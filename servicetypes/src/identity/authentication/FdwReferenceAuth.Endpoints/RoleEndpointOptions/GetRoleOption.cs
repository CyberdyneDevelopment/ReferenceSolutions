using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RoleEndpointOptions;

/// <summary>The GetRole endpoint.</summary>
[WebMcpTool("get_role", "Get role by name", ReadOnly = true)]
[TypeOption(typeof(RoleEndpoints), "GetRole")]
public class GetRoleOption : RoleEndpointBase<GetRoleEndpoint>
{
}
