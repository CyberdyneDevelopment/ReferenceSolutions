using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>The GetCategoryTypes endpoint.</summary>
[WebMcpTool("get_category_types", "List the types of category.", ReadOnly = true)]
[TypeOption(typeof(CategoryEndpoints), "GetCategoryTypes")]
public class GetCategoryTypesOption : CategoryEndpointBase<GetCategoryTypesEndpoint>
{
}
