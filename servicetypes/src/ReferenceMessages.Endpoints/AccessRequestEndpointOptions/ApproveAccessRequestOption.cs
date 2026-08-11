using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The ApproveAccessRequest endpoint.</summary>
[TypeOption(typeof(AccessRequestEndpoints), "ApproveAccessRequest")]
public class ApproveAccessRequestOption : AccessRequestEndpointBase<ApproveAccessRequestEndpoint>
{
}
