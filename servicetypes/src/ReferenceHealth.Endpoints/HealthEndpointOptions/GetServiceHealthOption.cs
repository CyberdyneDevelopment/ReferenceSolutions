using Fdw.Collections.Attributes;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetServiceHealth endpoint.</summary>
[TypeOption(typeof(HealthEndpoints), "GetServiceHealth")]
public class GetServiceHealthOption : HealthEndpointBase<GetServiceHealthEndpoint>
{
}
