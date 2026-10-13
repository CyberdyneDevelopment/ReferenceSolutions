using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ListFieldMappingTransformations endpoint.</summary>
[WebMcpTool("list_field_mapping_transformations", "List field mapping transformations.", ReadOnly = true)]
[TypeOption(typeof(TransformationsEndpoints), "ListFieldMappingTransformations")]
public class ListFieldMappingTransformationsOption : TransformationsEndpointBase<ListFieldMappingTransformationsEndpoint>
{
}
