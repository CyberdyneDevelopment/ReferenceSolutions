using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;

/// <summary>The ListConfigurationInstances endpoint.</summary>
[WebMcpTool("list_configuration_instances", "List configuration instances.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationInstanceEndpoints), "ListConfigurationInstances")]
public class ListConfigurationInstancesOption : ConfigurationInstanceEndpointBase<ListConfigurationInstancesEndpoint>
{
}
