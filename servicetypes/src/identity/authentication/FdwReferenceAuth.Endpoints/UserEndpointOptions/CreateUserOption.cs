using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The CreateUser endpoint.</summary>
[WebMcpTool("create_user", "Create a new user")]
[TypeOption(typeof(UserEndpoints), "CreateUser")]
public class CreateUserOption : UserEndpointBase<CreateUserEndpoint>
{
}
