using Fdw.Collections.Attributes;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The GetSessionState endpoint.</summary>
[TypeOption(typeof(SessionStateEndpoints), "GetSessionState")]
public class GetSessionStateOption : SessionStateEndpointBase<GetSessionStateEndpoint>
{
}
