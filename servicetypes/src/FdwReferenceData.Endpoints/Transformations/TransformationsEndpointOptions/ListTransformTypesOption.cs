using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ListTransformTypes endpoint.</summary>
[WebMcpTool("list_transform_types", "List transform types.", ReadOnly = true)]
[TypeOption(typeof(TransformationsEndpoints), "ListTransformTypes")]
public class ListTransformTypesOption : TransformationsEndpointBase<ListTransformTypesEndpoint>
{
}
