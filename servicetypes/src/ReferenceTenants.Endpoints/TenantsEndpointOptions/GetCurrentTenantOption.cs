using Fdw.Collections.Attributes;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>The GetCurrentTenant endpoint.</summary>
[TypeOption(typeof(TenantsEndpoints), "GetCurrentTenant")]
public class GetCurrentTenantOption : TenantsEndpointBase<GetCurrentTenantEndpoint>
{
}
