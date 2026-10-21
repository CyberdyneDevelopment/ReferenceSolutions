using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The GiveOrganizationMemberRole endpoint (FDW-1593).</summary>
[WebMcpTool("give_organization_member_role", "Give a person a role in an organization.")]
[TypeOption(typeof(TenantsEndpoints), "GiveOrganizationMemberRole")]
public class GiveOrganizationMemberRoleOption : TenantsEndpointBase<GiveOrganizationMemberRoleEndpoint>
{
}
