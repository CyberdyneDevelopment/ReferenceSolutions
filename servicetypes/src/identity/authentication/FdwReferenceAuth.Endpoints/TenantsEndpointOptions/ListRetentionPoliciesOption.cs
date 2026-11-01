using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListRetentionPolicies endpoint (FDW-1575).</summary>
[WebMcpTool("list_retention_policies", "List the retention policies of an organization.")]
[TypeOption(typeof(TenantsEndpoints), "ListRetentionPolicies")]
public class ListRetentionPoliciesOption : TenantsEndpointBase<ListRetentionPoliciesEndpoint>
{
}
