using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The DenyAccessRequest endpoint.</summary>
[TypeOption(typeof(AccessRequestEndpoints), "DenyAccessRequest")]
public class DenyAccessRequestOption : AccessRequestEndpointBase<DenyAccessRequestEndpoint>
{
}
