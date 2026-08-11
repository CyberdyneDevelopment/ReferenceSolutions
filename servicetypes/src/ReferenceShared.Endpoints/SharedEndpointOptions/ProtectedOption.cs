using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The Protected endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "Protected")]
public class ProtectedOption : SharedEndpointBase<ProtectedEndpoint>
{
}
