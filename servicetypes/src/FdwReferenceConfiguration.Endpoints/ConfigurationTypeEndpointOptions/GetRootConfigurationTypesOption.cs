using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetRootConfigurationTypes endpoint.</summary>
[WebMcpTool("get_root_configuration_types", "Get root configuration types.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetRootConfigurationTypes")]
public class GetRootConfigurationTypesOption : ConfigurationTypeEndpointBase<GetRootConfigurationTypesEndpoint>
{
}
