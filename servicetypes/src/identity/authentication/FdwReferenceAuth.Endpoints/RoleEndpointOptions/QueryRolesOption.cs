using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.RoleEndpointOptions;

/// <summary>The QueryRoles endpoint.</summary>
[WebMcpTool("query_roles", "Query roles by filter, order, page and field", ReadOnly = true)]
[TypeOption(typeof(RoleEndpoints), "QueryRoles")]
public class QueryRolesOption : RoleEndpointBase<QueryRolesEndpoint>
{
}
