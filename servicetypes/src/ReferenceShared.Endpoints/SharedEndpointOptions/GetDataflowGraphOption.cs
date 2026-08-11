using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetDataflowGraph endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetDataflowGraph")]
public class GetDataflowGraphOption : SharedEndpointBase<GetDataflowGraphEndpoint>
{
}
