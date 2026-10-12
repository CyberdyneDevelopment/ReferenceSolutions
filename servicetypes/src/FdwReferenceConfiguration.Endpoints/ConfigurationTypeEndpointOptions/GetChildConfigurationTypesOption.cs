using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetChildConfigurationTypes endpoint.</summary>
[WebMcpTool("get_child_configuration_types", "Get child configuration types.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetChildConfigurationTypes")]
public class GetChildConfigurationTypesOption : ConfigurationTypeEndpointBase<GetChildConfigurationTypesEndpoint>
{
}
