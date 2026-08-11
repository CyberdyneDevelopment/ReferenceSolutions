using Fdw.Collections.Attributes;

namespace ReferenceConfiguration.Endpoints.ConfigurationCategoryEndpointOptions;

/// <summary>The GetConfigurationCategories endpoint.</summary>
[TypeOption(typeof(ConfigurationCategoryEndpoints), "GetConfigurationCategories")]
public class GetConfigurationCategoriesOption : ConfigurationCategoryEndpointBase<GetConfigurationCategoriesEndpoint>
{
}
