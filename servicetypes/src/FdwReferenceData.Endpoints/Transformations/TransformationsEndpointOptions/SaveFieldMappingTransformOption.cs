using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The SaveFieldMappingTransform endpoint.</summary>
[WebMcpTool("save_field_mapping_transform", "Save field mapping transform.")]
[TypeOption(typeof(TransformationsEndpoints), "SaveFieldMappingTransform")]
public class SaveFieldMappingTransformOption : TransformationsEndpointBase<SaveFieldMappingTransformEndpoint>
{
}
