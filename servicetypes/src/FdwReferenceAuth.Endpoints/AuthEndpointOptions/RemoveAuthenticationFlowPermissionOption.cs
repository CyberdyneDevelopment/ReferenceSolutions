using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The RemoveAuthenticationFlowPermission endpoint.</summary>
[WebMcpTool("remove_authentication_flow_permission", "Open a sign-in flow to every active user.")]
[TypeOption(typeof(AuthEndpoints), "RemoveAuthenticationFlowPermission")]
public class RemoveAuthenticationFlowPermissionOption : AuthEndpointBase<RemoveAuthenticationFlowPermissionEndpoint>
{
}
