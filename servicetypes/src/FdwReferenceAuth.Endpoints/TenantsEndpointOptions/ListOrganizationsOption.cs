using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListOrganizations endpoint.</summary>
[WebMcpTool("list_organizations", "List the organizations of the caller's tenant.", ReadOnly = true)]
[TypeOption(typeof(TenantsEndpoints), "ListOrganizations")]
public class ListOrganizationsOption : TenantsEndpointBase<ListOrganizationsEndpoint>
{
}
