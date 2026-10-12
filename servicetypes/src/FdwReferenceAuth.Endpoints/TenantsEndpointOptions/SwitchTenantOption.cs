using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SwitchTenant endpoint — re-issues the caller's token for another tenant they may act in.</summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. The SwitchTenant base
/// existed without it, which is why a user could never change the tenant row-level security reads.
/// </remarks>
[WebMcpTool("switch_tenant", "Switch the active tenant; returns a new access token for it.")]
[TypeOption(typeof(TenantsEndpoints), "SwitchTenant")]
public class SwitchTenantOption : TenantsEndpointBase<SwitchTenantEndpoint>
{
}
