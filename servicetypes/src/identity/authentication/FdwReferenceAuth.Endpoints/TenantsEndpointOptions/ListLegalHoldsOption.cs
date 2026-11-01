using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListLegalHolds endpoint (FDW-1575).</summary>
[WebMcpTool("list_legal_holds", "List the legal holds of an organization.")]
[TypeOption(typeof(TenantsEndpoints), "ListLegalHolds")]
public class ListLegalHoldsOption : TenantsEndpointBase<ListLegalHoldsEndpoint>
{
}
