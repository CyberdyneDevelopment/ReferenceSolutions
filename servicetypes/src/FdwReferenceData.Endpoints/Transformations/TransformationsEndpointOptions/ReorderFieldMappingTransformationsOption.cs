using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The ReorderFieldMappingTransformations endpoint.</summary>
[WebMcpTool("reorder_field_mapping_transformations", "Reorder field mapping transformations.")]
[TypeOption(typeof(TransformationsEndpoints), "ReorderFieldMappingTransformations")]
public class ReorderFieldMappingTransformationsOption : TransformationsEndpointBase<ReorderFieldMappingTransformationsEndpoint>
{
}
