using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The GrantConnection endpoint (FDW-1581).</summary>
[WebMcpTool("grant_connection", "Grant a tenant a document connection for a purpose.")]
[TypeOption(typeof(TenantsEndpoints), "GrantConnection")]
public class GrantConnectionOption : TenantsEndpointBase<GrantConnectionEndpoint>
{
}
