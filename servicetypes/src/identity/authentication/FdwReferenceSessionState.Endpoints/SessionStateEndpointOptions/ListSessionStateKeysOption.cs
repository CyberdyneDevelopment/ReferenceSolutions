using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The ListSessionStateKeys endpoint.</summary>
[WebMcpTool("list_session_state_keys", "List session state keys.", ReadOnly = true)]
[TypeOption(typeof(SessionStateEndpoints), "ListSessionStateKeys")]
public class ListSessionStateKeysOption : SessionStateEndpointBase<ListSessionStateKeysEndpoint>
{
}
