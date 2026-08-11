using Fdw.Collections.Attributes;

namespace ReferenceAnalytics.Endpoints.AnalyticsEndpointOptions;

/// <summary>The GetTopCalculations endpoint.</summary>
[TypeOption(typeof(AnalyticsEndpoints), "GetTopCalculations")]
public class GetTopCalculationsOption : AnalyticsEndpointBase<GetTopCalculationsEndpoint>
{
}
