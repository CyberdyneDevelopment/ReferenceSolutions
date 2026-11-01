using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The SetRetentionPolicy endpoint (FDW-1575).</summary>
[WebMcpTool("set_retention_policy", "Set the retention policy of a document type.")]
[TypeOption(typeof(TenantsEndpoints), "SetRetentionPolicy")]
public class SetRetentionPolicyOption : TenantsEndpointBase<SetRetentionPolicyEndpoint>
{
}
