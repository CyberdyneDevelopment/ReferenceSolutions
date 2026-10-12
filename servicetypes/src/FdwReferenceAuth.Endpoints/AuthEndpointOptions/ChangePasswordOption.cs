using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ChangePassword endpoint.</summary>
[WebMcpTool("change_password", "Change password.")]
[TypeOption(typeof(AuthEndpoints), "ChangePassword")]
public class ChangePasswordOption : AuthEndpointBase<ChangePasswordEndpoint>
{
}
