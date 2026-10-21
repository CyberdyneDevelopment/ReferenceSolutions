using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListOrganizationRoles endpoint (FDW-1593).</summary>
[WebMcpTool("list_organization_roles", "List the roles of an organization.", ReadOnly = true)]
[TypeOption(typeof(TenantsEndpoints), "ListOrganizationRoles")]
public class ListOrganizationRolesOption : TenantsEndpointBase<ListOrganizationRolesEndpoint>
{
}
