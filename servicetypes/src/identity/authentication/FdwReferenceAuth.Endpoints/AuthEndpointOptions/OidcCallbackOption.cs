using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the OIDC callback (login completion) endpoint.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and the external provider's redirect back has nowhere to land.
/// </remarks>
[WebMcpTool("oidc_callback", "Complete a login after the external OIDC provider redirects back.")]
[TypeOption(typeof(AuthEndpoints), "OidcCallback")]
public class OidcCallbackOption : AuthEndpointBase<OidcCallbackEndpoint>
{
}
