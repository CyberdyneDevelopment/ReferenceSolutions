using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The CreateTenant endpoint: makes a tenant with its first administrator (FDW-1578).</summary>
[WebMcpTool("create_tenant", "Make a tenant together with its first administrator.")]
[TypeOption(typeof(TenantsEndpoints), "CreateTenant")]
public class CreateTenantOption : TenantsEndpointBase<CreateTenantEndpoint>
{
}
