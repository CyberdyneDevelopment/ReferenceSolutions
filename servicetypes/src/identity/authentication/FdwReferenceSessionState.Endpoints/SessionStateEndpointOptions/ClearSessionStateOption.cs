using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The ClearSessionState endpoint.</summary>
[WebMcpTool("clear_session_state", "Clear session state.")]
[TypeOption(typeof(SessionStateEndpoints), "ClearSessionState")]
public class ClearSessionStateOption : SessionStateEndpointBase<ClearSessionStateEndpoint>
{
}
