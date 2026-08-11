using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetConfigurationTypeDetail endpoint.</summary>
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetConfigurationTypeDetail")]
public class GetConfigurationTypeDetailOption : ConfigurationTypeEndpointBase<GetConfigurationTypeDetailEndpoint>
{
}
