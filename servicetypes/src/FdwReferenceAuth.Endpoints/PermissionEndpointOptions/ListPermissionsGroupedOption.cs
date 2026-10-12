using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.PermissionEndpointOptions;

/// <summary>The ListPermissionsGrouped endpoint.</summary>
[WebMcpTool("list_permissions_grouped", "List permissions grouped by resource", ReadOnly = true)]
[TypeOption(typeof(PermissionEndpoints), "ListPermissionsGrouped")]
public class ListPermissionsGroupedOption : PermissionEndpointBase<ListPermissionsGroupedEndpoint>
{
}
