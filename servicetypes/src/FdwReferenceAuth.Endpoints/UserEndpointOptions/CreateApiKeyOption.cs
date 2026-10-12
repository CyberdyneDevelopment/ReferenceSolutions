using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The CreateApiKey endpoint.</summary>
[WebMcpTool("create_api_key", "Mint an API key for an account.")]
[TypeOption(typeof(UserEndpoints), "CreateApiKey")]
public class CreateApiKeyOption : UserEndpointBase<CreateApiKeyEndpoint>
{
}
