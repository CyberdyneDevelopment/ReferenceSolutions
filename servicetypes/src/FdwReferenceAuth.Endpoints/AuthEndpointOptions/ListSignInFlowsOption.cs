using Fdw.Collections.Attributes;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>
/// Declares the sign-in flow list — GET /auth/sign-in-flows?returnUrl=.
/// </summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. The API reference's sign-in
/// panel reads it to decide which buttons and forms to draw.
/// </remarks>
[TypeOption(typeof(AuthEndpoints), "ListSignInFlows")]
public class ListSignInFlowsOption : AuthEndpointBase<ListSignInFlowsEndpoint>
{
}
