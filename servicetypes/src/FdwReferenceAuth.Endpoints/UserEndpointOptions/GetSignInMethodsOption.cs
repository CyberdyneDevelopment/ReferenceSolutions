using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The GetSignInMethods endpoint.</summary>
[WebMcpTool("get_sign_in_methods", "List an account's sign-in methods.", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "GetSignInMethods")]
public class GetSignInMethodsOption : UserEndpointBase<GetSignInMethodsEndpoint>
{
}
