using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The RestoreTenant endpoint (FDW-1759).</summary>
[WebMcpTool("restore_tenant", "Restore a deleted company.")]
[TypeOption(typeof(TenantsEndpoints), "RestoreTenant")]
public class RestoreTenantOption : TenantsEndpointBase<RestoreTenantEndpoint>
{
}
