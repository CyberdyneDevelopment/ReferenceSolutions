using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The UpdateTenant endpoint: renames, disables, enables or re-connects a tenant (FDW-1578).</summary>
[WebMcpTool("update_tenant", "Rename, disable, enable or re-connect a tenant.")]
[TypeOption(typeof(TenantsEndpoints), "UpdateTenant")]
public class UpdateTenantOption : TenantsEndpointBase<UpdateTenantEndpoint>
{
}
