using Fdw.Collections.Attributes;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The UpsertSessionState endpoint.</summary>
[TypeOption(typeof(SessionStateEndpoints), "UpsertSessionState")]
public class UpsertSessionStateOption : SessionStateEndpointBase<UpsertSessionStateEndpoint>
{
}
