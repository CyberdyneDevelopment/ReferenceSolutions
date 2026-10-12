using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The GetConfigurationTypeDetail endpoint.</summary>
[WebMcpTool("get_configuration_type_detail", "Get configuration type detail.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationTypeEndpoints), "GetConfigurationTypeDetail")]
public class GetConfigurationTypeDetailOption : ConfigurationTypeEndpointBase<GetConfigurationTypeDetailEndpoint>
{
}
