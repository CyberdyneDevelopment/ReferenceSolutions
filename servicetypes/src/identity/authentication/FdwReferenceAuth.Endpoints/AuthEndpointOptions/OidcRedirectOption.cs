using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the OIDC redirect (login start) endpoint.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and every attempt to start a federated login 404s.
/// </remarks>
[WebMcpTool("oidc_redirect", "Start a login through the configured external OIDC provider.")]
[TypeOption(typeof(AuthEndpoints), "OidcRedirect")]
public class OidcRedirectOption : AuthEndpointBase<OidcRedirectEndpoint>
{
}
