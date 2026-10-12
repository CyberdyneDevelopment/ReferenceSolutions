using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The DeleteFieldMappingTransform endpoint.</summary>
[WebMcpTool("delete_field_mapping_transform", "Delete field mapping transform.")]
[TypeOption(typeof(TransformationsEndpoints), "DeleteFieldMappingTransform")]
public class DeleteFieldMappingTransformOption : TransformationsEndpointBase<DeleteFieldMappingTransformEndpoint>
{
}
