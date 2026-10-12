using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The CreateAccessRequest endpoint.</summary>
[WebMcpTool("create_access_request", "Create access request.")]
[TypeOption(typeof(AccessRequestEndpoints), "CreateAccessRequest")]
public class CreateAccessRequestOption : AccessRequestEndpointBase<CreateAccessRequestEndpoint>
{
}
