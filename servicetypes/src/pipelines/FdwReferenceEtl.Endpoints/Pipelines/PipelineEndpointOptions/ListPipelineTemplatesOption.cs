using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ListPipelineTemplates endpoint.</summary>
[WebMcpTool("list_pipeline_templates", "List pipeline templates.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "ListPipelineTemplates")]
public class ListPipelineTemplatesOption : PipelineEndpointBase<ListPipelineTemplatesEndpoint>
{
}
