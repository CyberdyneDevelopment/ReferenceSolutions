using Fdw.Collections.Attributes;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetSystemHealth endpoint.</summary>
[TypeOption(typeof(HealthEndpoints), "GetSystemHealth")]
public class GetSystemHealthOption : HealthEndpointBase<GetSystemHealthEndpoint>
{
}
