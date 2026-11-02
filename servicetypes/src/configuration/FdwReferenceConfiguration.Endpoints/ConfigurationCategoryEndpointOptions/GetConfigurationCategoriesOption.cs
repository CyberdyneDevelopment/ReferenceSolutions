using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConfiguration.Endpoints.ConfigurationCategoryEndpointOptions;

/// <summary>The GetConfigurationCategories endpoint.</summary>
[WebMcpTool("get_configuration_categories", "Get configuration categories.", ReadOnly = true)]
[TypeOption(typeof(ConfigurationCategoryEndpoints), "GetConfigurationCategories")]
public class GetConfigurationCategoriesOption : ConfigurationCategoryEndpointBase<GetConfigurationCategoriesEndpoint>
{
}
