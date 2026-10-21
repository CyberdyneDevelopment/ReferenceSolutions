using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ValidateGraph endpoint.</summary>
[WebMcpTool("validate_graph_pipeline", "Check a graph pipeline draft.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "ValidateGraph")]
public class ValidateGraphOption : PipelineEndpointBase<ValidateGraphEndpoint>
{
}
