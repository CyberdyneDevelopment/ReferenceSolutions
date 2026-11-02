using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the one-time sign-in code redeem endpoint.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and every browser returning from a redirect sign-in gets a 404 on redeem.
/// </remarks>
[WebMcpTool("auth_redeem", "Redeem the one-time code a redirect sign-in returned the browser with for an access token.")]
[TypeOption(typeof(AuthEndpoints), "Redeem")]
public class RedeemOption : AuthEndpointBase<RedeemEndpoint>
{
}
