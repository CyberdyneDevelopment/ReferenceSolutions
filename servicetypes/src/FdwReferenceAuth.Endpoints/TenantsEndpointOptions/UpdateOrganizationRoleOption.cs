using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The UpdateOrganizationRole endpoint (FDW-1593).</summary>
[WebMcpTool("update_organization_role", "Change the description and permissions of a role of an organization.")]
[TypeOption(typeof(TenantsEndpoints), "UpdateOrganizationRole")]
public class UpdateOrganizationRoleOption : TenantsEndpointBase<UpdateOrganizationRoleEndpoint>
{
}
