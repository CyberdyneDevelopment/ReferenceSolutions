using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The GetUserMemberships endpoint.</summary>
/// <remarks>
/// Without this declaration the endpoint class compiles and the route 404s: auto-discovery is
/// off, so the option is what maps it.
/// </remarks>
[WebMcpTool("get_user_memberships", "List the tenants an account belongs to and the roles it holds in each.", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "GetUserMemberships")]
public class GetUserMembershipsOption : UserEndpointBase<GetUserMembershipsEndpoint>
{
}
