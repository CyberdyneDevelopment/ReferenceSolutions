using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the password login endpoint.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and every login 404s.
/// </remarks>
[WebMcpTool("password_login", "Sign in with a username and password.")]
[TypeOption(typeof(AuthEndpoints), "PasswordLogin")]
public class PasswordLoginOption : AuthEndpointBase<PasswordLoginEndpoint>
{
}
