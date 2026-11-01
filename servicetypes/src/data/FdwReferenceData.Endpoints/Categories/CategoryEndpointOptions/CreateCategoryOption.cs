using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>The CreateCategory endpoint.</summary>
[WebMcpTool("create_category", "Create a category.")]
[TypeOption(typeof(CategoryEndpoints), "CreateCategory")]
public class CreateCategoryOption : CategoryEndpointBase<CreateCategoryEndpoint>
{
}
