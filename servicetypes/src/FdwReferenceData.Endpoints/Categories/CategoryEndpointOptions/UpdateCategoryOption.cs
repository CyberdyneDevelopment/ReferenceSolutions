using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.CategoryEndpointOptions;

/// <summary>The UpdateCategory endpoint.</summary>
[WebMcpTool("update_category", "Rename, describe or move a category.")]
[TypeOption(typeof(CategoryEndpoints), "UpdateCategory")]
public class UpdateCategoryOption : CategoryEndpointBase<UpdateCategoryEndpoint>
{
}
