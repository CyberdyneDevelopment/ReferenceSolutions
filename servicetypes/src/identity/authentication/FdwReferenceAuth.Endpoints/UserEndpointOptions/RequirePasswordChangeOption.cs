using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The RequirePasswordChange endpoint.</summary>
[WebMcpTool("require_password_change", "Require a user to change their password at next login.")]
[TypeOption(typeof(UserEndpoints), "RequirePasswordChange")]
public class RequirePasswordChangeOption : UserEndpointBase<RequirePasswordChangeEndpoint>
{
}
