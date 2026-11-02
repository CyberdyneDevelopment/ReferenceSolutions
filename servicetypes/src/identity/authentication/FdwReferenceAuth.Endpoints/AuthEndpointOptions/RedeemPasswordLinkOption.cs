using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the endpoint a set-password link posts to when its holder chooses a password (FDW-941).
/// </summary>
[WebMcpTool("redeem_password_link", "Set a password using a one-time password link.")]
[TypeOption(typeof(AuthEndpoints), "RedeemPasswordLink")]
public class RedeemPasswordLinkOption : AuthEndpointBase<RedeemPasswordLinkEndpoint>
{
}
