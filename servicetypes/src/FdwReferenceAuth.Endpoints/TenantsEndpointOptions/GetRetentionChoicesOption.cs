using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The GetRetentionChoices endpoint (FDW-2906).</summary>
[WebMcpTool("get_retention_choices", "Read the start fields, end actions and document types an organization can choose for a retention policy.")]
[TypeOption(typeof(TenantsEndpoints), "GetRetentionChoices")]
public class GetRetentionChoicesOption : TenantsEndpointBase<GetRetentionChoicesEndpoint>
{
}
