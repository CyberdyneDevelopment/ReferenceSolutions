using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.PermissionEndpointOptions;

/// <summary>The QueryPermissions endpoint.</summary>
[WebMcpTool("query_permissions", "Query permissions by filter, order, page and field", ReadOnly = true)]
[TypeOption(typeof(PermissionEndpoints), "QueryPermissions")]
public class QueryPermissionsOption : PermissionEndpointBase<QueryPermissionsEndpoint>
{
}
