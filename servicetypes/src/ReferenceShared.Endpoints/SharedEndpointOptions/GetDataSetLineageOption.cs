using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetDataSetLineage endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetDataSetLineage")]
public class GetDataSetLineageOption : SharedEndpointBase<GetDataSetLineageEndpoint>
{
}
