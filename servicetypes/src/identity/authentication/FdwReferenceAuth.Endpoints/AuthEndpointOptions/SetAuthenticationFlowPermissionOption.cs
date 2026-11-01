using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The SetAuthenticationFlowPermission endpoint.</summary>
[WebMcpTool("set_authentication_flow_permission", "Limit a sign-in flow to users holding a permission.")]
[TypeOption(typeof(AuthEndpoints), "SetAuthenticationFlowPermission")]
public class SetAuthenticationFlowPermissionOption : AuthEndpointBase<SetAuthenticationFlowPermissionEndpoint>
{
}
