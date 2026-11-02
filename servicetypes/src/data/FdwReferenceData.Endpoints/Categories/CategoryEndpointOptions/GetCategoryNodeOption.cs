using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>The GetCategoryNode endpoint.</summary>
[WebMcpTool("get_category", "Get a category.", ReadOnly = true)]
[TypeOption(typeof(CategoryEndpoints), "GetCategoryNode")]
public class GetCategoryNodeOption : CategoryEndpointBase<GetCategoryNodeEndpoint>
{
}
