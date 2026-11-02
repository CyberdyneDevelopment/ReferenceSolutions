using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The CreateTransformation endpoint.</summary>
[WebMcpTool("create_transformation", "Define a transformation.")]
[TypeOption(typeof(TransformationsEndpoints), "CreateTransformation")]
public class CreateTransformationOption : TransformationsEndpointBase<CreateTransformationEndpoint>
{
}
