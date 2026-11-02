using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ListTransformations endpoint.</summary>
[WebMcpTool("list_transformations", "List transformations.", ReadOnly = true)]
[TypeOption(typeof(TransformationsEndpoints), "ListTransformations")]
public class ListTransformationsOption : TransformationsEndpointBase<ListTransformationsEndpoint>
{
}
