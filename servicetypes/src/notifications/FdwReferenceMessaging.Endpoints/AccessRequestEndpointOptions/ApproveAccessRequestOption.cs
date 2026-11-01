using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The ApproveAccessRequest endpoint.</summary>
[WebMcpTool("approve_access_request", "Approve access request.")]
[TypeOption(typeof(AccessRequestEndpoints), "ApproveAccessRequest")]
public class ApproveAccessRequestOption : AccessRequestEndpointBase<ApproveAccessRequestEndpoint>
{
}
