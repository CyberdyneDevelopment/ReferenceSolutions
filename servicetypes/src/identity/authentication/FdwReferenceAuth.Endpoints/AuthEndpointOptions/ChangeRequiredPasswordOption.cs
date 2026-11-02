using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the endpoint a user whose sign-in answered PasswordChangeRequired uses to set a new
/// password.
/// </summary>
[WebMcpTool("change_required_password", "Change a password that must be changed before signing in.")]
[TypeOption(typeof(AuthEndpoints), "ChangeRequiredPassword")]
public class ChangeRequiredPasswordOption : AuthEndpointBase<ChangeRequiredPasswordEndpoint>
{
}
