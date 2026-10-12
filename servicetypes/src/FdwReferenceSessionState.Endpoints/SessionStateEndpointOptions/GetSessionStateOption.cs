using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The GetSessionState endpoint.</summary>
[WebMcpTool("get_session_state", "Get session state.", ReadOnly = true)]
[TypeOption(typeof(SessionStateEndpoints), "GetSessionState")]
public class GetSessionStateOption : SessionStateEndpointBase<GetSessionStateEndpoint>
{
}
