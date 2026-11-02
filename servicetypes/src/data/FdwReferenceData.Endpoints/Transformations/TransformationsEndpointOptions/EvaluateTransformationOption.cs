using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The EvaluateTransformation endpoint.</summary>
[WebMcpTool("evaluate_transformation", "Evaluate a Built transformation with values for its inputs.", ReadOnly = true)]
[TypeOption(typeof(TransformationsEndpoints), "EvaluateTransformation")]
public class EvaluateTransformationOption : TransformationsEndpointBase<EvaluateTransformationEndpoint>
{
}
