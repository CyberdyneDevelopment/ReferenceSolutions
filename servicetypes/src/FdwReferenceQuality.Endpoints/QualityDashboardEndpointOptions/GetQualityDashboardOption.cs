using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityDashboardEndpointOptions;

/// <summary>The GetQualityDashboard endpoint.</summary>
[WebMcpTool("get_quality_dashboard", "Get quality dashboard.", ReadOnly = true)]
[TypeOption(typeof(QualityDashboardEndpoints), "GetQualityDashboard")]
public class GetQualityDashboardOption : QualityDashboardEndpointBase<GetQualityDashboardEndpoint>
{
}
