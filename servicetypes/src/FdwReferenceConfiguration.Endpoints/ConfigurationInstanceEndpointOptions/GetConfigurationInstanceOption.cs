using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;

/// <summary>The GetConfigurationInstance endpoint.</summary>
[WebMcpTool("get_configuration_instance", "Get configuration instance.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationInstanceEndpoints), "GetConfigurationInstance")]
public class GetConfigurationInstanceOption : ConfigurationInstanceEndpointBase<GetConfigurationInstanceEndpoint>
{
}
