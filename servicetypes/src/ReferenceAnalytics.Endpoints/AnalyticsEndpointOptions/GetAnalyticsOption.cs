using Fdw.Collections.Attributes;

namespace ReferenceAnalytics.Endpoints.AnalyticsEndpointOptions;

/// <summary>The GetAnalytics endpoint.</summary>
[TypeOption(typeof(AnalyticsEndpoints), "GetAnalytics")]
public class GetAnalyticsOption : AnalyticsEndpointBase<GetAnalyticsEndpoint>
{
}
