using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The RevokeApiKey endpoint.</summary>
[WebMcpTool("revoke_api_key", "Revoke one of an account's API keys.")]
[TypeOption(typeof(UserEndpoints), "RevokeApiKey")]
public class RevokeApiKeyOption : UserEndpointBase<RevokeApiKeyEndpoint>
{
}
