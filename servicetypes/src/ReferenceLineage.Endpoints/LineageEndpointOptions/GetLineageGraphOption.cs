using Fdw.Collections.Attributes;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>The GetLineageGraph endpoint.</summary>
[TypeOption(typeof(LineageEndpoints), "GetLineageGraph")]
public class GetLineageGraphOption : LineageEndpointBase<GetLineageGraphEndpoint>
{
}
