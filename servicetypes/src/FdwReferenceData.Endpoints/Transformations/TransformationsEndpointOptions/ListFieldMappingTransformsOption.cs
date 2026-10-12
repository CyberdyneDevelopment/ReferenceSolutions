using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ListFieldMappingTransforms endpoint.</summary>
[WebMcpTool("list_field_mapping_transforms", "List field mapping transforms.", ReadOnly = true)]
[TypeOption(typeof(TransformationsEndpoints), "ListFieldMappingTransforms")]
public class ListFieldMappingTransformsOption : TransformationsEndpointBase<ListFieldMappingTransformsEndpoint>
{
}
