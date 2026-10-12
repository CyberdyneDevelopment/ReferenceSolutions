using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The ListAccessRequests endpoint.</summary>
[WebMcpTool("list_access_requests", "List access requests.", ReadOnly = true)]
[TypeOption(typeof(AccessRequestEndpoints), "ListAccessRequests")]
public class ListAccessRequestsOption : AccessRequestEndpointBase<ListAccessRequestsEndpoint>
{
}
