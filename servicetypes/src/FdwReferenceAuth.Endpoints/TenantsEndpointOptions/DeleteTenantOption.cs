using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The DeleteTenant endpoint (FDW-1759).</summary>
[WebMcpTool("delete_tenant", "Delete a company.")]
[TypeOption(typeof(TenantsEndpoints), "DeleteTenant")]
public class DeleteTenantOption : TenantsEndpointBase<DeleteTenantEndpoint>
{
}
