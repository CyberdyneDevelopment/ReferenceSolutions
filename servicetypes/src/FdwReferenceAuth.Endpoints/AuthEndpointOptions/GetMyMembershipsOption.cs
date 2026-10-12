using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The GetMyMemberships endpoint.</summary>
/// <remarks>
/// Without this declaration the endpoint class compiles and the route 404s — auto-discovery is
/// off, so the option is what maps it.
/// </remarks>
[WebMcpTool("get_my_memberships", "List the tenants and organisations I belong to", ReadOnly = true)]
[TypeOption(typeof(AuthEndpoints), "GetMyMemberships")]
public class GetMyMembershipsOption : AuthEndpointBase<GetMyMembershipsEndpoint>
{
}
