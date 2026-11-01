using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The RemovePasswordLogin endpoint.</summary>
[WebMcpTool("remove_password_login", "Take an account's password login away.")]
[TypeOption(typeof(UserEndpoints), "RemovePasswordLogin")]
public class RemovePasswordLoginOption : UserEndpointBase<RemovePasswordLoginEndpoint>
{
}
