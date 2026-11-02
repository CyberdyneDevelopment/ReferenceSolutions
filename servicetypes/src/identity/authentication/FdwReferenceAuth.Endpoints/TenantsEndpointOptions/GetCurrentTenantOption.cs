using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The GetCurrentTenant endpoint.</summary>
[WebMcpTool("get_current_tenant", "Get current tenant.", ReadOnly = true)]
[TypeOption(typeof(TenantsEndpoints), "GetCurrentTenant")]
public class GetCurrentTenantOption : TenantsEndpointBase<GetCurrentTenantEndpoint>
{
}
