using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ListPipelineRunQueue endpoint.</summary>
[WebMcpTool("list_pipeline_run_queue", "List the pipeline run queue.", ReadOnly = true)]
[TypeOption(typeof(PipelineEndpoints), "ListPipelineRunQueue")]
public class ListPipelineRunQueueOption : PipelineEndpointBase<ListPipelineRunQueueEndpoint>
{
}
