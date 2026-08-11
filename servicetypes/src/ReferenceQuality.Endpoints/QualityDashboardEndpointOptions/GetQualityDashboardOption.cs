using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityDashboardEndpointOptions;

/// <summary>The GetQualityDashboard endpoint.</summary>
[TypeOption(typeof(QualityDashboardEndpoints), "GetQualityDashboard")]
public class GetQualityDashboardOption : QualityDashboardEndpointBase<GetQualityDashboardEndpoint>
{
}
