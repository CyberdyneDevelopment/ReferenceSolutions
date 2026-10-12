using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The UpdateAuthenticationFlow endpoint.</summary>
[WebMcpTool("update_authentication_flow", "Switch a sign-in flow on or off.")]
[TypeOption(typeof(AuthEndpoints), "UpdateAuthenticationFlow")]
public class UpdateAuthenticationFlowOption : AuthEndpointBase<UpdateAuthenticationFlowEndpoint>
{
}
