using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The ListAccessRequests endpoint.</summary>
[TypeOption(typeof(AccessRequestEndpoints), "ListAccessRequests")]
public class ListAccessRequestsOption : AccessRequestEndpointBase<ListAccessRequestsEndpoint>
{
}
