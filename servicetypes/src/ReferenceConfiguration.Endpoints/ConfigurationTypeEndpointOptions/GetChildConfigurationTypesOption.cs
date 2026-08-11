using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetChildConfigurationTypes endpoint.</summary>
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetChildConfigurationTypes")]
public class GetChildConfigurationTypesOption : ConfigurationTypeEndpointBase<GetChildConfigurationTypesEndpoint>
{
}
