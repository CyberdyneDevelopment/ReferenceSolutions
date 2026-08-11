using Fdw.Collections.Attributes;

namespace ReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The CreateAccessRequest endpoint.</summary>
[TypeOption(typeof(AccessRequestEndpoints), "CreateAccessRequest")]
public class CreateAccessRequestOption : AccessRequestEndpointBase<CreateAccessRequestEndpoint>
{
}
