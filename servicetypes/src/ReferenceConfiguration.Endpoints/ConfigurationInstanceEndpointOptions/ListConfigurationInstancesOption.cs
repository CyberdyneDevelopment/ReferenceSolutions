using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;

/// <summary>The ListConfigurationInstances endpoint.</summary>
[TypeOption(typeof(ConfigurationInstanceEndpoints), "ListConfigurationInstances")]
public class ListConfigurationInstancesOption : ConfigurationInstanceEndpointBase<ListConfigurationInstancesEndpoint>
{
}
