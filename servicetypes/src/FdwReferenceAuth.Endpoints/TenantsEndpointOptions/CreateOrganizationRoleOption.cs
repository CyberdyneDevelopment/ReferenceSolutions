using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The CreateOrganizationRole endpoint (FDW-1593).</summary>
[WebMcpTool("create_organization_role", "Define a role in an organization.")]
[TypeOption(typeof(TenantsEndpoints), "CreateOrganizationRole")]
public class CreateOrganizationRoleOption : TenantsEndpointBase<CreateOrganizationRoleEndpoint>
{
}
