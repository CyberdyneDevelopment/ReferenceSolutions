using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The UpdateTransformation endpoint.</summary>
[WebMcpTool("update_transformation", "Change a transformation.")]
[TypeOption(typeof(TransformationsEndpoints), "UpdateTransformation")]
public class UpdateTransformationOption : TransformationsEndpointBase<UpdateTransformationEndpoint>
{
}
