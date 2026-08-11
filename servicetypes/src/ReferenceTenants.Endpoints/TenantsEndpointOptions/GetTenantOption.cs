using Fdw.Collections.Attributes;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>The GetTenant endpoint.</summary>
[TypeOption(typeof(TenantsEndpoints), "GetTenant")]
public class GetTenantOption : TenantsEndpointBase<GetTenantEndpoint>
{
}
