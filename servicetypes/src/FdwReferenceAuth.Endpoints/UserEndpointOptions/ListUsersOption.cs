using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The ListUsers endpoint.</summary>
[WebMcpTool("list_users", "List all users", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "ListUsers")]
public class ListUsersOption : UserEndpointBase<ListUsersEndpoint>
{
}
