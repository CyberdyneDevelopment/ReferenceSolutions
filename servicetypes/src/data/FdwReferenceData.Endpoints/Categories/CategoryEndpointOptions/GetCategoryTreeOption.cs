using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>The GetCategoryTree endpoint.</summary>
[WebMcpTool("get_category_tree", "Get the category tree.", ReadOnly = true)]
[TypeOption(typeof(CategoryEndpoints), "GetCategoryTree")]
public class GetCategoryTreeOption : CategoryEndpointBase<GetCategoryTreeEndpoint>
{
}
