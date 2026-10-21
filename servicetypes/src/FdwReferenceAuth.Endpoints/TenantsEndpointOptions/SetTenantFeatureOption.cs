using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SetTenantFeature endpoint (FDW-1580).</summary>
[WebMcpTool("set_tenant_feature", "Switch a feature on or off for a tenant.")]
[TypeOption(typeof(TenantsEndpoints), "SetTenantFeature")]
public class SetTenantFeatureOption : TenantsEndpointBase<SetTenantFeatureEndpoint>
{
}
