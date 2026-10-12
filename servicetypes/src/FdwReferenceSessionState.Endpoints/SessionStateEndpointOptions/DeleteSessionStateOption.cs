using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The DeleteSessionState endpoint.</summary>
[WebMcpTool("delete_session_state", "Delete session state.")]
[TypeOption(typeof(SessionStateEndpoints), "DeleteSessionState")]
public class DeleteSessionStateOption : SessionStateEndpointBase<DeleteSessionStateEndpoint>
{
}
