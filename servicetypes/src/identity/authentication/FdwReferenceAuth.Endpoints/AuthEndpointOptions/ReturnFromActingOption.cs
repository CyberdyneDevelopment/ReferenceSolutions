using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ReturnFromActing endpoint (FDW-2282).</summary>
[WebMcpTool("return_from_acting", "Stop acting with a role and go back to one of your earlier sessions.")]
[TypeOption(typeof(AuthEndpoints), "ReturnFromActing")]
public class ReturnFromActingOption : AuthEndpointBase<ReturnFromActingEndpoint>
{
}
