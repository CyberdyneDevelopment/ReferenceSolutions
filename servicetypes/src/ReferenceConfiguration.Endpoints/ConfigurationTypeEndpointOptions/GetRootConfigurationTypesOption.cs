using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetRootConfigurationTypes endpoint.</summary>
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetRootConfigurationTypes")]
public class GetRootConfigurationTypesOption : ConfigurationTypeEndpointBase<GetRootConfigurationTypesEndpoint>
{
}
