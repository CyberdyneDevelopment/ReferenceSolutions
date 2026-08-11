using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;

/// <summary>The GetConfigurationInstance endpoint.</summary>
[TypeOption(typeof(ConfigurationInstanceEndpoints), "GetConfigurationInstance")]
public class GetConfigurationInstanceOption : ConfigurationInstanceEndpointBase<GetConfigurationInstanceEndpoint>
{
}
