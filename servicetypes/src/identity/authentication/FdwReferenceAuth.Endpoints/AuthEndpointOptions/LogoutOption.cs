using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The Logout endpoint.</summary>
[WebMcpTool("logout", "Logout.")]
[TypeOption(typeof(AuthEndpoints), "Logout")]
public class LogoutOption : AuthEndpointBase<LogoutEndpoint>
{
}
