using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The CreateGraphFromTemplate endpoint.</summary>
[WebMcpTool("create_graph_pipeline_from_template", "Make a graph pipeline from a template.")]
[TypeOption(typeof(PipelineEndpoints), "CreateGraphFromTemplate")]
public class CreateGraphFromTemplateOption : PipelineEndpointBase<CreateGraphFromTemplateEndpoint>
{
}
