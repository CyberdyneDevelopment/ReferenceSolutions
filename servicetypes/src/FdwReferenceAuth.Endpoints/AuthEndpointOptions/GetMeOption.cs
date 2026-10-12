using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The GetMe endpoint.</summary>
[WebMcpTool("get_me", "Get current user", ReadOnly = true)]
[TypeOption(typeof(AuthEndpoints), "GetMe")]
public class GetMeOption : AuthEndpointBase<GetMeEndpoint>
{
}
