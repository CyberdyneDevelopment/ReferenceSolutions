using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ReorderFieldMappingTransforms endpoint.</summary>
[WebMcpTool("reorder_field_mapping_transforms", "Reorder field mapping transforms.")]
[TypeOption(typeof(TransformationsEndpoints), "ReorderFieldMappingTransforms")]
public class ReorderFieldMappingTransformsOption : TransformationsEndpointBase<ReorderFieldMappingTransformsEndpoint>
{
}
