using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The ResetPassword endpoint.</summary>
[WebMcpTool("reset_password", "Reset password.")]
[TypeOption(typeof(UserEndpoints), "ResetPassword")]
public class ResetPasswordOption : UserEndpointBase<ResetPasswordEndpoint>
{
}
