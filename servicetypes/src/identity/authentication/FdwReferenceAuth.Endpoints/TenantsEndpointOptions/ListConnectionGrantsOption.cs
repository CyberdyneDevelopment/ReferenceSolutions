using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListConnectionGrants endpoint (FDW-1581).</summary>
[WebMcpTool("list_connection_grants", "List the document connections granted to a tenant.")]
[TypeOption(typeof(TenantsEndpoints), "ListConnectionGrants")]
public class ListConnectionGrantsOption : TenantsEndpointBase<ListConnectionGrantsEndpoint>
{
}
