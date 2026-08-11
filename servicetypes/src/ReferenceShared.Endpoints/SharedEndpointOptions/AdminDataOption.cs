using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The AdminData endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "AdminData")]
public class AdminDataOption : SharedEndpointBase<AdminDataEndpoint>
{
}
