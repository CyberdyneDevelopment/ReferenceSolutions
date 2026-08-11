using Fdw.Collections.Attributes;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The ListSessionStateKeys endpoint.</summary>
[TypeOption(typeof(SessionStateEndpoints), "ListSessionStateKeys")]
public class ListSessionStateKeysOption : SessionStateEndpointBase<ListSessionStateKeysEndpoint>
{
}
