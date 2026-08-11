using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The GetImpactAnalysis endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "GetImpactAnalysis")]
public class GetImpactAnalysisOption : SharedEndpointBase<GetImpactAnalysisEndpoint>
{
}
