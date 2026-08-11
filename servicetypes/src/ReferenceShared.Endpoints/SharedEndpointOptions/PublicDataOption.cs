using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The PublicData endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "PublicData")]
public class PublicDataOption : SharedEndpointBase<PublicDataEndpoint>
{
}
