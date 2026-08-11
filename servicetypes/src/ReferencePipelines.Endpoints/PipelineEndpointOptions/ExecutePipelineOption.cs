using Fdw.Collections.Attributes;

namespace ReferencePipelines.Endpoints.PipelineEndpointOptions;

/// <summary>The ExecutePipeline endpoint.</summary>
[TypeOption(typeof(PipelineEndpoints), "ExecutePipeline")]
public class ExecutePipelineOption : PipelineEndpointBase<ExecutePipelineEndpoint>
{
}
