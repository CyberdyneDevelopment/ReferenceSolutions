using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The IssueSessionCsrfToken endpoint (FDW-903).</summary>
[WebMcpTool("auth_session_csrf", "Issue a CSRF token for the current server-side session.")]
[TypeOption(typeof(AuthEndpoints), "IssueSessionCsrfToken")]
public class IssueSessionCsrfTokenOption : AuthEndpointBase<IssueSessionCsrfTokenEndpoint>
{
}
