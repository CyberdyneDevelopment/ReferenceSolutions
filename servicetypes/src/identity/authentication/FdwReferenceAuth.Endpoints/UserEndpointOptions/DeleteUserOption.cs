using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The DeleteUser endpoint.</summary>
[WebMcpTool("delete_user", "Delete a user")]
[TypeOption(typeof(UserEndpoints), "DeleteUser")]
public class DeleteUserOption : UserEndpointBase<DeleteUserEndpoint>
{
}
