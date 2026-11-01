using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The UpdateUser endpoint.</summary>
[WebMcpTool("update_user", "Update a user")]
[TypeOption(typeof(UserEndpoints), "UpdateUser")]
public class UpdateUserOption : UserEndpointBase<UpdateUserEndpoint>
{
}
