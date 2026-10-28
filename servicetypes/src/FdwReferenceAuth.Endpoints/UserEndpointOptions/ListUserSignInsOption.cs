using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The ListUserSignIns endpoint.</summary>
[WebMcpTool("list_user_sign_ins", "List an account's sign-ins: when, from where, in which browser.", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "ListUserSignIns")]
public class ListUserSignInsOption : UserEndpointBase<ListUserSignInsEndpoint>
{
}
