using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The SetPasswordLogin endpoint.</summary>
[WebMcpTool("set_password_login", "Give an account its first password.")]
[TypeOption(typeof(UserEndpoints), "SetPasswordLogin")]
public class SetPasswordLoginOption : UserEndpointBase<SetPasswordLoginEndpoint>
{
}
