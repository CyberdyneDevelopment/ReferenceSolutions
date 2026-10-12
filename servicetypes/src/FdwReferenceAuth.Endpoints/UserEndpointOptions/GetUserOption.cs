using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The GetUser endpoint.</summary>
[WebMcpTool("get_user", "Get user by ID or name", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "GetUser")]
public class GetUserOption : UserEndpointBase<GetUserEndpoint>
{
}
