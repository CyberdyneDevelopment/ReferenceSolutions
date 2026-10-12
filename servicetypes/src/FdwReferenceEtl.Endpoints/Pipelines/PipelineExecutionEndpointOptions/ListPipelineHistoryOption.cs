using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>The ListPipelineHistory endpoint.</summary>
[WebMcpTool("list_pipeline_history", "List pipeline history.", ReadOnly = true)]
[TypeOption(typeof(PipelineExecutionEndpoints), "ListPipelineHistory")]
public class ListPipelineHistoryOption : PipelineExecutionEndpointBase<ListPipelineHistoryEndpoint>
{
}
