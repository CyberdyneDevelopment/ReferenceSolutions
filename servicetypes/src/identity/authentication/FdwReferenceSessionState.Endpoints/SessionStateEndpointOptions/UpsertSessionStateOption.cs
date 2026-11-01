using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The UpsertSessionState endpoint.</summary>
[WebMcpTool("upsert_session_state", "Upsert session state.")]
[TypeOption(typeof(SessionStateEndpoints), "UpsertSessionState")]
public class UpsertSessionStateOption : SessionStateEndpointBase<UpsertSessionStateEndpoint>
{
}
