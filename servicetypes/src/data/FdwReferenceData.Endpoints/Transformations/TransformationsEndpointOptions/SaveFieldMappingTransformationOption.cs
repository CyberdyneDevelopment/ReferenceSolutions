using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The SaveFieldMappingTransformation endpoint.</summary>
[WebMcpTool("save_field_mapping_transformation", "Save field mapping transformation.")]
[TypeOption(typeof(TransformationsEndpoints), "SaveFieldMappingTransformation")]
public class SaveFieldMappingTransformationOption : TransformationsEndpointBase<SaveFieldMappingTransformationEndpoint>
{
}
