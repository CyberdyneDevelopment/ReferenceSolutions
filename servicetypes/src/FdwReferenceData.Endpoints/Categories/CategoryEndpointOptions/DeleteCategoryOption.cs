using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>The DeleteCategory endpoint.</summary>
[WebMcpTool("delete_category", "Retire a category.")]
[TypeOption(typeof(CategoryEndpoints), "DeleteCategory")]
public class DeleteCategoryOption : CategoryEndpointBase<DeleteCategoryEndpoint>
{
}
