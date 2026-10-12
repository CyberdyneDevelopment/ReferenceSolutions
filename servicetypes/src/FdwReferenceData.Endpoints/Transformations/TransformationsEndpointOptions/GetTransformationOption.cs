using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The GetTransformation endpoint.</summary>
[WebMcpTool("get_transformation", "Get a transformation by name.", ReadOnly = true)]
[TypeOption(typeof(TransformationsEndpoints), "GetTransformation")]
public class GetTransformationOption : TransformationsEndpointBase<GetTransformationEndpoint>
{
}
