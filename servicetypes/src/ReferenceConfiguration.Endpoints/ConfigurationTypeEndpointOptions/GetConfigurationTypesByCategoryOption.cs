using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetConfigurationTypesByCategory endpoint.</summary>
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetConfigurationTypesByCategory")]
public class GetConfigurationTypesByCategoryOption : ConfigurationTypeEndpointBase<GetConfigurationTypesByCategoryEndpoint>
{
}
