using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The AssignOrganizationConnection endpoint (FDW-1581).</summary>
[WebMcpTool("assign_organization_connection", "Assign an organization a document connection for a purpose.")]
[TypeOption(typeof(TenantsEndpoints), "AssignOrganizationConnection")]
public class AssignOrganizationConnectionOption : TenantsEndpointBase<AssignOrganizationConnectionEndpoint>
{
}
