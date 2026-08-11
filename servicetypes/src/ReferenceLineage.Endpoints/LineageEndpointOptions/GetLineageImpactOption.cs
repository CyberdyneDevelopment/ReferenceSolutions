using Fdw.Collections.Attributes;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>The GetLineageImpact endpoint.</summary>
[TypeOption(typeof(LineageEndpoints), "GetLineageImpact")]
public class GetLineageImpactOption : LineageEndpointBase<GetLineageImpactEndpoint>
{
}
