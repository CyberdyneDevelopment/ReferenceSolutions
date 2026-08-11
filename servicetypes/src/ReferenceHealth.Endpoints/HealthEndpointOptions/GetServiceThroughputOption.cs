using Fdw.Collections.Attributes;

namespace ReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetServiceThroughput endpoint.</summary>
[TypeOption(typeof(HealthEndpoints), "GetServiceThroughput")]
public class GetServiceThroughputOption : HealthEndpointBase<GetServiceThroughputEndpoint>
{
}
