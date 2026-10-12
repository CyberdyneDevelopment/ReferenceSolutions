using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetConfigurationTypesByCategory endpoint.</summary>
[WebMcpTool("get_configuration_types_by_category", "Get configuration types by category.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetConfigurationTypesByCategory")]
public class GetConfigurationTypesByCategoryOption : ConfigurationTypeEndpointBase<GetConfigurationTypesByCategoryEndpoint>
{
}
