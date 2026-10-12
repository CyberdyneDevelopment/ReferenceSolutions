using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The DeleteTransformation endpoint.</summary>
[WebMcpTool("delete_transformation", "Delete a transformation.")]
[TypeOption(typeof(TransformationsEndpoints), "DeleteTransformation")]
public class DeleteTransformationOption : TransformationsEndpointBase<DeleteTransformationEndpoint>
{
}
