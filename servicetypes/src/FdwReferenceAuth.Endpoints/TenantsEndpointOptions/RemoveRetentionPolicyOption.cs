using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The RemoveRetentionPolicy endpoint (FDW-1575).</summary>
[WebMcpTool("remove_retention_policy", "Remove the retention policy of a document type.")]
[TypeOption(typeof(TenantsEndpoints), "RemoveRetentionPolicy")]
public class RemoveRetentionPolicyOption : TenantsEndpointBase<RemoveRetentionPolicyEndpoint>
{
}
