using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The DeleteOrganizationRole endpoint (FDW-1593).</summary>
[WebMcpTool("delete_organization_role", "Delete a role of an organization.")]
[TypeOption(typeof(TenantsEndpoints), "DeleteOrganizationRole")]
public class DeleteOrganizationRoleOption : TenantsEndpointBase<DeleteOrganizationRoleEndpoint>
{
}
