using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The PlaceLegalHold endpoint (FDW-1575).</summary>
[WebMcpTool("place_legal_hold", "Put a document under a legal hold.")]
[TypeOption(typeof(TenantsEndpoints), "PlaceLegalHold")]
public class PlaceLegalHoldOption : TenantsEndpointBase<PlaceLegalHoldEndpoint>
{
}
