using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The LiftLegalHold endpoint (FDW-1575).</summary>
[WebMcpTool("lift_legal_hold", "Lift the legal hold on a document.")]
[TypeOption(typeof(TenantsEndpoints), "LiftLegalHold")]
public class LiftLegalHoldOption : TenantsEndpointBase<LiftLegalHoldEndpoint>
{
}
