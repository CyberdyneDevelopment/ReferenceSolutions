using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListOrganizationMembers endpoint (FDW-1593).</summary>
[WebMcpTool("list_organization_members", "List the people in an organization.", ReadOnly = true)]
[TypeOption(typeof(TenantsEndpoints), "ListOrganizationMembers")]
public class ListOrganizationMembersOption : TenantsEndpointBase<ListOrganizationMembersEndpoint>
{
}
