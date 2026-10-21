using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The RunRetention endpoint (FDW-1575).</summary>
[WebMcpTool("run_retention", "Run the retention job for an organization.")]
[TypeOption(typeof(TenantsEndpoints), "RunRetention")]
public class RunRetentionOption : TenantsEndpointBase<RunRetentionEndpoint>
{
}
