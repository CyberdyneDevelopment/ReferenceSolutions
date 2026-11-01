using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The CreatePasswordLink endpoint (FDW-941).</summary>
[WebMcpTool("create_password_link", "Create a one-time link that lets a user set their password.")]
[TypeOption(typeof(UserEndpoints), "CreatePasswordLink")]
public class CreatePasswordLinkOption : UserEndpointBase<CreatePasswordLinkEndpoint>
{
}
