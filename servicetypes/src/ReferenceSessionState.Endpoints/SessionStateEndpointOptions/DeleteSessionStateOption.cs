using Fdw.Collections.Attributes;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The DeleteSessionState endpoint.</summary>
[TypeOption(typeof(SessionStateEndpoints), "DeleteSessionState")]
public class DeleteSessionStateOption : SessionStateEndpointBase<DeleteSessionStateEndpoint>
{
}
