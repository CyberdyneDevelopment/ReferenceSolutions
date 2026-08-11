using Fdw.Collections.Attributes;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The ClearSessionState endpoint.</summary>
[TypeOption(typeof(SessionStateEndpoints), "ClearSessionState")]
public class ClearSessionStateOption : SessionStateEndpointBase<ClearSessionStateEndpoint>
{
}
