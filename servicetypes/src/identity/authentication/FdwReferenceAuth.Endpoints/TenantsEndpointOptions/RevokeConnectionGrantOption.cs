using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The RevokeConnectionGrant endpoint (FDW-1581).</summary>
[WebMcpTool("revoke_connection_grant", "Take a document connection back from a tenant.")]
[TypeOption(typeof(TenantsEndpoints), "RevokeConnectionGrant")]
public class RevokeConnectionGrantOption : TenantsEndpointBase<RevokeConnectionGrantEndpoint>
{
}
