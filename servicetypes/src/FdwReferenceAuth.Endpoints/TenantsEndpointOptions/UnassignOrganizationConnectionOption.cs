using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The UnassignOrganizationConnection endpoint (FDW-1581).</summary>
[WebMcpTool("unassign_organization_connection", "Take a purpose's document connection away from an organization.")]
[TypeOption(typeof(TenantsEndpoints), "UnassignOrganizationConnection")]
public class UnassignOrganizationConnectionOption : TenantsEndpointBase<UnassignOrganizationConnectionEndpoint>
{
}
