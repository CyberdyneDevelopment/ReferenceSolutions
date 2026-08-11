using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineExecutionEndpointOptions;

/// <summary>The ListPipelineHistory endpoint.</summary>
[TypeOption(typeof(PipelineExecutionEndpoints), "ListPipelineHistory")]
public class ListPipelineHistoryOption : PipelineExecutionEndpointBase<ListPipelineHistoryEndpoint>
{
}
