using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The DeleteFieldMappingTransformation endpoint.</summary>
[WebMcpTool("delete_field_mapping_transformation", "Delete field mapping transformation.")]
[TypeOption(typeof(TransformationsEndpoints), "DeleteFieldMappingTransformation")]
public class DeleteFieldMappingTransformationOption : TransformationsEndpointBase<DeleteFieldMappingTransformationEndpoint>
{
}
