using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ListAuthenticationFlows endpoint.</summary>
[WebMcpTool("list_authentication_flows", "List sign-in flows: on or off, served or broken, what each proves and the permission it requires.")]
[TypeOption(typeof(AuthEndpoints), "ListAuthenticationFlows")]
public class ListAuthenticationFlowsOption : AuthEndpointBase<ListAuthenticationFlowsEndpoint>
{
}
