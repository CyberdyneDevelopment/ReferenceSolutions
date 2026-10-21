using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListOrganizationConnections endpoint (FDW-1581).</summary>
[WebMcpTool("list_organization_connections", "List the document connections assigned to an organization.")]
[TypeOption(typeof(TenantsEndpoints), "ListOrganizationConnections")]
public class ListOrganizationConnectionsOption : TenantsEndpointBase<ListOrganizationConnectionsEndpoint>
{
}
