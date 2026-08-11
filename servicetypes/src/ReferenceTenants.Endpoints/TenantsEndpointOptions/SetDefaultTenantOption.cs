using Fdw.Collections.Attributes;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>The SetDefaultTenant endpoint.</summary>
[TypeOption(typeof(TenantsEndpoints), "SetDefaultTenant")]
public class SetDefaultTenantOption : TenantsEndpointBase<SetDefaultTenantEndpoint>
{
}
