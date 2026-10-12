using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the token exchange endpoint.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and every attempt to exchange a foreign token 404s.
/// </remarks>
[WebMcpTool("token_exchange", "Exchange an already-issued foreign token (Entra, Auth0, any OIDC provider) for one of this host's own.")]
[TypeOption(typeof(AuthEndpoints), "TokenExchange")]
public class TokenExchangeOption : AuthEndpointBase<TokenExchangeEndpoint>
{
}
