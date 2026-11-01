using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SetDefaultTenant endpoint.</summary>
[WebMcpTool("set_default_tenant", "Set default tenant.")]
[TypeOption(typeof(TenantsEndpoints), "SetDefaultTenant")]
public class SetDefaultTenantOption : TenantsEndpointBase<SetDefaultTenantEndpoint>
{
}
