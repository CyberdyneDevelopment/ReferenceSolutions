using Fdw.Collections.Attributes;

namespace ReferenceTenants.Endpoints.TenantsEndpointOptions;

/// <summary>The ListTenants endpoint.</summary>
[TypeOption(typeof(TenantsEndpoints), "ListTenants")]
public class ListTenantsOption : TenantsEndpointBase<ListTenantsEndpoint>
{
}
