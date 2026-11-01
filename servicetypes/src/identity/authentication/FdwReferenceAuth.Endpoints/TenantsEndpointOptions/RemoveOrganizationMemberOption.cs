using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The RemoveOrganizationMember endpoint (FDW-1593).</summary>
[WebMcpTool("remove_organization_member", "Remove a person from an organization.")]
[TypeOption(typeof(TenantsEndpoints), "RemoveOrganizationMember")]
public class RemoveOrganizationMemberOption : TenantsEndpointBase<RemoveOrganizationMemberEndpoint>
{
}
