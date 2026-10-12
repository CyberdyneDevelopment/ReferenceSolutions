using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The GetTenant endpoint.</summary>
[WebMcpTool("get_tenant", "Get tenant.", ReadOnly = true)]
[TypeOption(typeof(TenantsEndpoints), "GetTenant")]
public class GetTenantOption : TenantsEndpointBase<GetTenantEndpoint>
{
}
