using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The DenyAccessRequest endpoint.</summary>
[WebMcpTool("deny_access_request", "Deny access request.")]
[TypeOption(typeof(AccessRequestEndpoints), "DenyAccessRequest")]
public class DenyAccessRequestOption : AccessRequestEndpointBase<DenyAccessRequestEndpoint>
{
}
