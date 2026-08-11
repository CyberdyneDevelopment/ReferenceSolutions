using Fdw.Collections.Attributes;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetServiceHealthHistory endpoint.</summary>
[TypeOption(typeof(HealthEndpoints), "GetServiceHealthHistory")]
public class GetServiceHealthHistoryOption : HealthEndpointBase<GetServiceHealthHistoryEndpoint>
{
}
