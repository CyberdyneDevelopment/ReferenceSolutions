using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ListActingRoles endpoint (FDW-2282).</summary>
[WebMcpTool("list_acting_roles", "List the roles you may act with.")]
[TypeOption(typeof(AuthEndpoints), "ListActingRoles")]
public class ListActingRolesOption : AuthEndpointBase<ListActingRolesEndpoint>
{
}
