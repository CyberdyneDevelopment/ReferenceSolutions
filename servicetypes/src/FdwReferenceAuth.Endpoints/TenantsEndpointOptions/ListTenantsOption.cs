using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListTenants endpoint.</summary>
[WebMcpTool("list_tenants", "List tenants.", ReadOnly = true)]
[TypeOption(typeof(TenantsEndpoints), "ListTenants")]
public class ListTenantsOption : TenantsEndpointBase<ListTenantsEndpoint>
{
}
