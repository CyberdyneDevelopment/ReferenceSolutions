using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.UserEndpointOptions;

/// <summary>The ListApiKeys endpoint.</summary>
[WebMcpTool("list_api_keys", "List an account's API keys.", ReadOnly = true)]
[TypeOption(typeof(UserEndpoints), "ListApiKeys")]
public class ListApiKeysOption : UserEndpointBase<ListApiKeysEndpoint>
{
}
